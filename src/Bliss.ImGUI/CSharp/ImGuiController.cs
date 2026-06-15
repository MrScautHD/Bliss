using System.Numerics;
using System.Runtime.InteropServices;
using Bliss.CSharp;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Graphics;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Images;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Interact.Mice;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Windowing;
using Bliss.CSharp.Windowing.Events;
using Bliss.ImGUI.CSharp.VertexTypes;
using Hexa.NET.ImGui;
using Veldrith;
using Veldrith.SPIRV;
using Sdl = SDL3.SDL;

namespace Bliss.ImGUI.CSharp;

public class ImGuiController : Disposable {
    
    /// <summary>
    /// Gets the graphics device used to create rendering resources and execute draw commands.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// Gets the window that ImGui receives input events from and renders into.
    /// </summary>
    public IWindow Window { get; private set; }
    
    /// <summary>
    /// Gets the current maximum number of vertices the buffers can hold before being resized.
    /// </summary>
    public uint Capacity { get; private set; }
    
    /// <summary>
    /// The ImGui IO state providing input, timing, and configuration flags.
    /// </summary>
    public ImGuiIOPtr Io { get; private set; }
    
    /// <summary>
    /// The ImGui style state controlling appearance and font sizing.
    /// </summary>
    public ImGuiStylePtr Style { get; private set; }
    
    /// <summary>
    /// The dynamic buffer holding the ImGui vertex data for the current frame.
    /// </summary>
    private DeviceBuffer _vertexBuffer;
    
    /// <summary>
    /// The dynamic buffer holding the ImGui index data for the current frame.
    /// </summary>
    private DeviceBuffer _indexBuffer;
    
    /// <summary>
    /// The uniform buffer storing the orthographic projection and view matrices used by the vertex shader.
    /// </summary>
    private SimpleUniformBuffer<Matrix4x4> _projViewBuffer;
    
    /// <summary>
    /// The pipeline description defining blend, depth, and rasterizer state for rendering ImGui draw data.
    /// </summary>
    private SimplePipelineDescription _pipelineDescription;
    
    /// <summary>
    /// Indicates whether a frame has been begun and not yet ended.
    /// </summary>
    private bool _begun;
    
    /// <summary>
    /// Indicates whether SDL text input is currently enabled by this controller.
    /// </summary>
    private bool _textInputEnabled;
    
    /// <summary>
    /// The command list used to record rendering commands for the current frame.
    /// </summary>
    private CommandList _commandList;
    
    /// <summary>
    /// The output description defining the render target the current frame is drawn into.
    /// </summary>
    private OutputDescription _output;
    
    /// <summary>
    /// The effect providing the shaders and resource layouts used to render ImGui draw data.
    /// </summary>
    private Effect _effect;
    
    /// <summary>
    /// The texture holding the current ImGui font atlas.
    /// </summary>
    private Texture2D _fontTexture;
    
    /// <summary>
    /// The ImGui texture identifier bound to the font texture.
    /// </summary>
    private ImTextureID _fontTextureId;
    
    /// <summary>
    /// Maps texture identifiers to their bound texture and sampler pairs for use during rendering.
    /// </summary>
    private Dictionary<ulong, (Texture2D Texture, Sampler Sampler)> _textures;
    
    /// <summary>
    /// The next texture identifier to assign when binding a new texture.
    /// </summary>
    private uint _textureIds;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ImGuiController"/> class, creating the rendering resources, ImGui context, and registering the window input event handlers.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used to create buffers, textures, and the rendering pipeline.</param>
    /// <param name="window">The window ImGui receives input events from and renders into.</param>
    /// <param name="capacity">The initial maximum number of vertices the buffers can hold before being resized.</param>
    public ImGuiController(GraphicsDevice graphicsDevice, IWindow window, uint capacity = 65536) {
        this.GraphicsDevice = graphicsDevice;
        this.Window = window;
        this.Capacity = capacity;
        
        // Create vertex buffer.
        uint vertexBufferSize = capacity * (uint) Marshal.SizeOf<ImGuiVertex>();
        this._vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        this._vertexBuffer.Name = "VertexBuffer";
        
        // Create index buffer.
        uint indexBufferSize = capacity * 3 * sizeof(ushort);
        this._indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        this._indexBuffer.Name = "IndexBuffer";
        
        // Create projection view buffer.
        this._projViewBuffer = new SimpleUniformBuffer<Matrix4x4>(graphicsDevice, 2, ShaderStages.Vertex);
        this._projViewBuffer.DeviceBuffer.Name = "ProjViewBuffer";
        
        // Create pipeline description.
        this._pipelineDescription = new SimplePipelineDescription() {
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            BlendState = BlendStateDescription.SINGLE_ALPHA_BLEND,
            DepthStencilState = DepthStencilStateDescription.DISABLED,
            RasterizerState = RasterizerStateDescription.CULL_NONE with {
                ScissorTestEnabled = true
            }
        };
        
        // Create effect.
        this._effect = new Effect(graphicsDevice, "content/imgui/shaders/imgui_default.vert", "content/imgui/shaders/imgui_default.frag", new CrossCompileOptions());
        this._effect.AddBufferLayout("ProjectionViewBuffer", 0, SimpleBufferType.Uniform, ShaderStages.Vertex);
        this._effect.AddTextureLayout("fTexture", 1);
        
        // Create texture cache.
        this._textures = new Dictionary<ulong, (Texture2D Texture, Sampler Sampler)>();
        this._textureIds = 1;
        
        // Create ImGUI context.
        ImGuiContextPtr contextPtr = ImGui.CreateContext();
        ImGui.SetCurrentContext(contextPtr);
        
        this.Io = ImGui.GetIO();
        this.Io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard
                                | ImGuiConfigFlags.NavEnableGamepad
                                | ImGuiConfigFlags.DockingEnable;
        
        this.Io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors
                                 | ImGuiBackendFlags.HasSetMousePos
                                 | ImGuiBackendFlags.RendererHasVtxOffset
                                 | ImGuiBackendFlags.RendererHasTextures;
        
        this.Style = ImGui.GetStyle();
        this.Io.Fonts.RendererHasTextures = true;
        
        if ((this.Io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0) {
            this.Style.WindowRounding = 0.0F;
            this.Style.Colors[(int) ImGuiCol.WindowBg].W = 1.0F;
        }
        
        // Create font texture.
        this._fontTexture = this.CreateFontTexture();
        
        // Add window events.
        this.Window.Resized += this.OnWindowResized;
        this.Window.MouseMove += this.OnMouseMove;
        this.Window.MouseWheel += this.OnMouseWheel;
        this.Window.MouseButtonDown += this.OnMouseButtonDown;
        this.Window.MouseButtonUp += this.OnMouseButtonUp;
        this.Window.KeyDown += this.OnKeyDown;
        this.Window.KeyUp += this.OnKeyUp;
        this.Window.TextInput += this.OnTextInput;
    }
    
    /// <summary>
    /// Prepares the ImGui frame by initializing the necessary command list and output description.
    /// </summary>
    /// <param name="commandList">The command list used for rendering ImGui frames.</param>
    /// <param name="output">The output description defining the target rendering context.</param>
    public void Begin(CommandList commandList, OutputDescription output) {
        if (this._begun) {
            throw new Exception("The ImGuiController has already begun!");
        }
        
        this._begun = true;
        
        this._commandList = commandList;
        this._output = output;
        
        this.UpdateIo();
        ImGui.NewFrame();
    }
    
    /// <summary>
    /// Finalizes the ImGui frame by rendering the collected draw data and updating text input state.
    /// </summary>
    /// <exception cref="Exception">Thrown if the method is called before the ImGui frame is begun.</exception>
    public void End() {
        if (!this._begun) {
            throw new Exception("The ImGuiController has not begun yet!");
        }
        
        // Draw ImGUI.
        ImGui.Render();
        this.UpdateTextInput();
        this.RenderDrawData(ImGui.GetDrawData());
        
        this._begun = false;
    }
    
    /// <summary>
    /// Updates the ImGui frame, adjusting the delta time and ensuring the font texture is updated if necessary.
    /// </summary>
    /// <param name="deltaTime">The time in seconds since the last update. If zero or negative, a default value is used.</param>
    public void Update(double deltaTime) {
        this.Io.DeltaTime = deltaTime > 0.0 ? (float) deltaTime : 1.0F / 60.0F;
        this.UpdateFontTexture();
    }
    
    /// <summary>
    /// Binds a given texture to an ImGui context and generates a unique texture ID for it.
    /// </summary>
    /// <param name="texture">The texture to be bound to the ImGui context.</param>
    /// <param name="sampler">The sampler to use when rendering the texture. If null, the default ImGui sampler is used.</param>
    /// <returns>A unique ImTextureID representing the bound texture.</returns>
    public ImTextureID BindTexture(Texture2D texture, Sampler? sampler = null) {
        ulong id = this._textureIds++;
        
        this._textures.Add(id, (texture, sampler ?? GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.PointClamp)));
        return new ImTextureID(id);
    }
    
    /// <summary>
    /// Unbinds a texture from the ImGui rendering context and removes it from the internal texture dictionary.
    /// </summary>
    /// <param name="textureId">The identifier of the texture to be unbound and removed.</param>
    public void UnbindTexture(ImTextureID textureId) {
        this._textures.Remove(textureId.Handle);
    }
    
    /// <summary>
    /// Renders the given ImGui draw data by configuring buffers, updating the pipeline, and executing drawing commands for all the draw lists contained within the draw data.
    /// </summary>
    /// <param name="drawData">A pointer to the ImGui draw data containing the draw lists, vertices, indices, and commands to be rendered.</param>
    private unsafe void RenderDrawData(ImDrawDataPtr drawData) {
        if (drawData.IsNull || drawData.TotalVtxCount == 0) {
            return;
        }
        
        // Ensure buffer capacity.
        this.EnsureBufferCapacity((uint) drawData.TotalVtxCount, (uint) drawData.TotalIdxCount);
        
        // Update projection/view buffer.
        Vector2 displayPosition = drawData.DisplayPos;
        Vector2 displaySize = drawData.DisplaySize;
        Vector2 framebufferScale = drawData.FramebufferScale;
        Vector2 framebufferSize = displaySize * framebufferScale;
        
        this._projViewBuffer.SetValue(0, Matrix4x4.CreateOrthographicOffCenter(displayPosition.X, displayPosition.X + displaySize.X, displayPosition.Y + displaySize.Y, displayPosition.Y, -1.0F, 1.0F));
        this._projViewBuffer.SetValue(1, Matrix4x4.Identity);
        this._projViewBuffer.UpdateBufferDeferred(this._commandList);
        
        // Update pipeline description.
        this._pipelineDescription.BufferLayouts = this._effect.GetBufferLayouts();
        this._pipelineDescription.TextureLayouts = this._effect.GetTextureLayouts();
        this._pipelineDescription.ShaderSet = new ShaderSetDescription(ImGuiVertex.VertexLayout.Layouts, this._effect.Shaders);
        this._pipelineDescription.Outputs = this._output;
        
        // Set vertex/index buffer.
        this._commandList.SetVertexBuffer(0, this._vertexBuffer);
        this._commandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt16);
        
        // Set pipeline.
        this._commandList.SetPipeline(this._effect.GetPipeline(this._pipelineDescription).Pipeline);
        
        // Set projection view buffer.
        this._commandList.SetGraphicsResourceSet(this._effect.GetBufferLayoutSlot("ProjectionViewBuffer"), this._projViewBuffer.GetResourceSet(this._effect.GetBufferLayout("ProjectionViewBuffer")));
        
        int vertexOffset = 0;
        uint indexOffset = 0;
        
        for (int i = 0; i < drawData.CmdListsCount; i++) {
            ImDrawListPtr cmdList = drawData.CmdLists[i];
            
            // Update vertex/index buffer.
            ImGuiVertex[] vertices = new ImGuiVertex[cmdList.VtxBuffer.Size];
            ImDrawVert* drawVertices = cmdList.VtxBuffer.Data;
            
            for (int j = 0; j < vertices.Length; j++) {
                ImDrawVert drawVertex = drawVertices[j];
                
                byte r = (byte) (drawVertex.Col & 0xFF);
                byte g = (byte) ((drawVertex.Col >> 8) & 0xFF);
                byte b = (byte) ((drawVertex.Col >> 16) & 0xFF);
                byte a = (byte) ((drawVertex.Col >> 24) & 0xFF);
                
                vertices[j] = new ImGuiVertex(new Vector3(drawVertex.Pos, 0.0F), drawVertex.Uv, new Vector4(r, g, b, a) / 255.0F);
            }
            
            // Update vertex/index buffer.
            this._commandList.UpdateBuffer(this._vertexBuffer, (uint) (vertexOffset * Marshal.SizeOf<ImGuiVertex>()), vertices);
            this._commandList.UpdateBuffer(this._indexBuffer, indexOffset * sizeof(ushort), new ReadOnlySpan<ushort>(cmdList.IdxBuffer.Data, cmdList.IdxBuffer.Size));
            
            uint cmdListIndexOffset = indexOffset;
            
            for (int j = 0; j < cmdList.CmdBuffer.Size; j++) {
                ImDrawCmd cmd = cmdList.CmdBuffer[j];
                
                if (cmd.UserCallback != (void*) nint.Zero) {
                    continue;
                }
                
                // Set scissor rect.
                Vector4 clipRect = cmd.ClipRect;
                clipRect.X = (clipRect.X - displayPosition.X) * framebufferScale.X;
                clipRect.Y = (clipRect.Y - displayPosition.Y) * framebufferScale.Y;
                clipRect.Z = (clipRect.Z - displayPosition.X) * framebufferScale.X;
                clipRect.W = (clipRect.W - displayPosition.Y) * framebufferScale.Y;
                
                uint scissorX = (uint) Math.Clamp(clipRect.X, 0.0F, framebufferSize.X);
                uint scissorY = (uint) Math.Clamp(clipRect.Y, 0.0F, framebufferSize.Y);
                uint scissorWidth = (uint) Math.Clamp(clipRect.Z - clipRect.X, 0.0F, framebufferSize.X - scissorX);
                uint scissorHeight = (uint) Math.Clamp(clipRect.W - clipRect.Y, 0.0F, framebufferSize.Y - scissorY);
                
                if (scissorWidth == 0 || scissorHeight == 0) {
                    continue;
                }
                
                this._commandList.SetScissorRect(0, scissorX, scissorY, scissorWidth, scissorHeight);
                
                // Set resourceSet of the texture.
                (Texture2D Texture, Sampler Sampler) texturePair = this._textures.GetValueOrDefault(cmd.GetTexID());
                this._commandList.SetGraphicsResourceSet(this._effect.GetTextureLayoutSlot("fTexture"), texturePair.Texture.GetResourceSet(texturePair.Sampler, this._effect.GetTextureLayout("fTexture")));
                
                // Apply effect.
                this._effect.Apply(this._commandList);
                
                // Draw.
                this._commandList.DrawIndexed(cmd.ElemCount, 1, cmdListIndexOffset + cmd.IdxOffset, vertexOffset + (int) cmd.VtxOffset, 0);
            }
            
            vertexOffset += cmdList.VtxBuffer.Size;
            indexOffset += (uint) cmdList.IdxBuffer.Size;
        }
        
        // Reset scissor.
        this._commandList.SetFullScissorRect(0);
    }
    
    /// <summary>
    /// Creates and initializes the default ImGui font texture.
    /// </summary>
    /// <returns>The created font texture.</returns>
    private unsafe Texture2D CreateFontTexture() {
        
        // Set font size.
        if (this.Style.FontSizeBase <= 0.0F) {
            this.Style.FontSizeBase = 16.0F;
        }
        
        // Create default font.
        this.Io.FontDefault = this.Io.Fonts.AddFontDefault();
        this.Io.FontDefault.GetFontBaked(this.Style.FontSizeBase);
        
        // Create fallback texture.
        this._fontTexture = new Texture2D(this.GraphicsDevice, new Image(1, 1, [255, 255, 255, 255]), false);
        this._fontTextureId = this.BindTexture(this._fontTexture, GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.LinearClamp));
        
        // Upload font atlas texture.
        return this.UpdateFontTexture();
    }
    
    /// <summary>
    /// Updates the ImGui font texture if the font atlas requests a texture upload or rebuild.
    /// </summary>
    /// <returns>The active font texture.</returns>
    private unsafe Texture2D UpdateFontTexture() {
        ImTextureDataPtr textureData = this.Io.Fonts.TexData;
        
        // Use fallback texture.
        if (textureData.IsNull) {
            return this._fontTexture;
        }
        
        // Keep uploaded texture.
        if (textureData.Status == ImTextureStatus.Ok) {
            return this._fontTexture;
        }
        
        // Destroy font texture.
        if (textureData.Status == ImTextureStatus.WantDestroy) {
            this.UnbindTexture(this._fontTextureId);
            this._fontTexture.Dispose();
            this._fontTexture = new Texture2D(this.GraphicsDevice, new Image(1, 1, [255, 255, 255, 255]), false);
            this._fontTextureId = this.BindTexture(this._fontTexture, GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.LinearClamp));
            
            textureData.SetStatus(ImTextureStatus.Destroyed);
            return this._fontTexture;
        }
        
        int width = textureData.Width;
        int height = textureData.Height;
        int bytesPerPixel = textureData.BytesPerPixel;
        
        // Validate atlas data.
        if (width <= 0 || height <= 0 || textureData.Pixels == null) {
            textureData.SetTexID(this._fontTextureId);
            textureData.SetStatus(ImTextureStatus.Ok);
            return this._fontTexture;
        }
        
        // Copy atlas pixels.
        byte[] source = new byte[width * height * bytesPerPixel];
        Marshal.Copy((nint) textureData.Pixels, source, 0, source.Length);
        
        byte[] data = bytesPerPixel == 4 ? source : new byte[width * height * 4];
        
        // Convert alpha atlas.
        if (bytesPerPixel == 1) {
            for (int i = 0; i < source.Length; i++) {
                int destIndex = i * 4;
                
                data[destIndex] = 255;
                data[destIndex + 1] = 255;
                data[destIndex + 2] = 255;
                data[destIndex + 3] = source[i];
            }
        }
        
        // Update existing texture.
        if (this._fontTexture.Width == width && this._fontTexture.Height == height) {
            this._fontTexture.SetData(new Image(width, height, data));
            textureData.SetTexID(this._fontTextureId);
            textureData.SetStatus(ImTextureStatus.Ok);
            return this._fontTexture;
        }
        
        // Recreate resized texture.
        this.UnbindTexture(this._fontTextureId);
        this._fontTexture.Dispose();
        
        this._fontTexture = new Texture2D(this.GraphicsDevice, new Image(width, height, data), false);
        this._fontTextureId = this.BindTexture(this._fontTexture, GraphicsHelper.GetSampler(this.GraphicsDevice, SamplerType.LinearClamp));
        
        textureData.SetTexID(this._fontTextureId);
        textureData.SetStatus(ImTextureStatus.Ok);
        
        return this._fontTexture;
    }
    
    /// <summary>
    /// Ensures the vertex and index buffers can hold the requested element counts, recreating them with increased capacity if the current size is insufficient.
    /// </summary>
    /// <param name="vertexCount">The number of vertices that must fit into the vertex buffer.</param>
    /// <param name="indexCount">The number of indices that must fit into the index buffer.</param>
    private void EnsureBufferCapacity(uint vertexCount, uint indexCount) {
        if (vertexCount <= this.Capacity && indexCount <= this.Capacity * 3) {
            return;
        }
        
        this.Capacity = Math.Max(this.Capacity * 2, Math.Max(vertexCount, (indexCount + 2) / 3));
        
        // Recreate vertex buffer.
        this._vertexBuffer.Dispose();
        uint vertexBufferSize = this.Capacity * (uint) Marshal.SizeOf<ImGuiVertex>();
        this._vertexBuffer = this.GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        this._vertexBuffer.Name = "VertexBuffer";
        
        // Recreate index buffer.
        this._indexBuffer.Dispose();
        uint indexBufferSize = this.Capacity * 3 * sizeof(ushort);
        this._indexBuffer = this.GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        this._indexBuffer.Name = "IndexBuffer";
    }
    
    /// <summary>
    /// Updates the ImGui IO state with the current window dimensions and frame timing data.
    /// </summary>
    private void UpdateIo() {
        this.Io.DisplaySize = new Vector2(this.Window.GetWidth(), this.Window.GetHeight());
        
        if (this.Io.DeltaTime <= 0.0F) {
            this.Io.DeltaTime = 1.0F / 60.0F;
        }
    }
    
    /// <summary>
    /// Manages the activation and deactivation of the SDL text input mode based on ImGui's text input state.
    /// Enables text input when requested by ImGui and disables it when no longer needed.
    /// </summary>
    private void UpdateTextInput() {
        bool textInputActive = Input.IsTextInputActive();
        
        if (this.Io.WantTextInput) {
            if (!textInputActive) {
                Input.EnableTextInput();
                this._textInputEnabled = true;
            }
            
            return;
        }
        
        if (this._textInputEnabled) {
            if (textInputActive) {
                Input.DisableTextInput();
            }
            
            this._textInputEnabled = false;
        }
    }

    /// <summary>
    /// Handles window resize events and updates the ImGui input/output system accordingly.
    /// </summary>
    private void OnWindowResized() {
        this.UpdateIo();
    }
    
    /// <summary>
    /// Handles mouse movement events and updates the ImGui input system with the cursor's current position.
    /// </summary>
    /// <param name="position">The current position of the mouse cursor, represented as a 2D vector.</param>
    private void OnMouseMove(Vector2 position) {
        this.Io.AddMousePosEvent(position.X, position.Y);
    }
    
    /// <summary>
    /// Handles mouse wheel movement events and updates the ImGui input system with the scrolling deltas.
    /// </summary>
    /// <param name="wheel">The mouse wheel delta values, containing the horizontal and vertical scrolling amounts.</param>
    private void OnMouseWheel(Vector2 wheel) {
        this.Io.AddMouseWheelEvent(wheel.X, wheel.Y);
    }
    
    /// <summary>
    /// Handles mouse button press events and updates the ImGui input system to register the button's press state.
    /// </summary>
    /// <param name="mouseEvent">The mouse event information, containing details about the pressed mouse button.</param>
    private void OnMouseButtonDown(MouseEvent mouseEvent) {
        int mouseButton = this.MapMouseButton(mouseEvent.Button);
        
        if (mouseButton != -1) {
            this.Io.AddMouseButtonEvent(mouseButton, true);
        }
    }
    
    /// <summary>
    /// Handles mouse button release events and updates the ImGui input system to register the button's release state.
    /// </summary>
    /// <param name="mouseEvent">The mouse event information, containing details about the released mouse button.</param>
    private void OnMouseButtonUp(MouseEvent mouseEvent) {
        int mouseButton = this.MapMouseButton(mouseEvent.Button);
        
        if (mouseButton != -1) {
            this.Io.AddMouseButtonEvent(mouseButton, false);
        }
    }
    
    /// <summary>
    /// Handles the key press events received from the window and updates the ImGui input system to reflect the key's pressed state.
    /// </summary>
    /// <param name="keyEvent">The key event information, containing details about the pressed keyboard key.</param>
    private void OnKeyDown(KeyEvent keyEvent) {
        ImGuiKey key = this.MapKeyboardKey(keyEvent.KeyboardKey);
        
        if (key != ImGuiKey.None) {
            this.Io.AddKeyEvent(key, true);
        }
    }
    
    /// <summary>
    /// Handles the key release events received from the window and updates the ImGui input system to reflect the key's release state.
    /// </summary>
    /// <param name="keyEvent">The key event information, containing details about the released keyboard key.</param>
    private void OnKeyUp(KeyEvent keyEvent) {
        ImGuiKey key = this.MapKeyboardKey(keyEvent.KeyboardKey);
        
        if (key != ImGuiKey.None) {
            this.Io.AddKeyEvent(key, false);
        }
    }
    
    /// <summary>
    /// Handles text input events received from the window and forwards the input characters to the ImGui input system.
    /// </summary>
    /// <param name="text">The text input received, typically a single or multiple UTF-8 characters.</param>
    private void OnTextInput(string text) {
        this.Io.AddInputCharactersUTF8(text);
    }
    
    /// <summary>
    /// Maps a given MouseButton enumeration value to the corresponding ImGuiMouseButton representation or a custom integer.
    /// </summary>
    /// <param name="button">The MouseButton to be mapped.</param>
    /// <returns>Returns the integer representation of the specified MouseButton, or -1 if the mapping is invalid.</returns>
    private int MapMouseButton(MouseButton button) {
        return button switch {
            MouseButton.Left => (int) ImGuiMouseButton.Left,
            MouseButton.Right => (int) ImGuiMouseButton.Right,
            MouseButton.Middle => (int) ImGuiMouseButton.Middle,
            MouseButton.X1 => 3,
            MouseButton.X2 => 4,
            _ => -1
        };
    }
    
    /// <summary>
    /// Maps a given keyboard key to an ImGuiKey representation.
    /// </summary>
    /// <param name="key">The keyboard key to be mapped.</param>
    /// <returns>Returns the corresponding ImGuiKey representation for the specified keyboard key.</returns>
    private ImGuiKey MapKeyboardKey(KeyboardKey key) {
        return key switch {
            KeyboardKey.Tab => ImGuiKey.Tab,
            KeyboardKey.Left => ImGuiKey.LeftArrow,
            KeyboardKey.Right => ImGuiKey.RightArrow,
            KeyboardKey.Up => ImGuiKey.UpArrow,
            KeyboardKey.Down => ImGuiKey.DownArrow,
            KeyboardKey.PageUp => ImGuiKey.PageUp,
            KeyboardKey.PageDown => ImGuiKey.PageDown,
            KeyboardKey.Home => ImGuiKey.Home,
            KeyboardKey.End => ImGuiKey.End,
            KeyboardKey.Insert => ImGuiKey.Insert,
            KeyboardKey.Delete => ImGuiKey.Delete,
            KeyboardKey.BackSpace => ImGuiKey.Backspace,
            KeyboardKey.Space => ImGuiKey.Space,
            KeyboardKey.Enter => ImGuiKey.Enter,
            KeyboardKey.Escape => ImGuiKey.Escape,
            KeyboardKey.Quote => ImGuiKey.Apostrophe,
            KeyboardKey.Comma => ImGuiKey.Comma,
            KeyboardKey.Minus => ImGuiKey.Minus,
            KeyboardKey.Period => ImGuiKey.Period,
            KeyboardKey.Slash => ImGuiKey.Slash,
            KeyboardKey.Semicolon => ImGuiKey.Semicolon,
            KeyboardKey.Plus => ImGuiKey.Equal,
            KeyboardKey.BracketLeft => ImGuiKey.LeftBracket,
            KeyboardKey.BackSlash => ImGuiKey.Backslash,
            KeyboardKey.BracketRight => ImGuiKey.RightBracket,
            KeyboardKey.Grave => ImGuiKey.GraveAccent,
            KeyboardKey.CapsLock => ImGuiKey.CapsLock,
            KeyboardKey.ScrollLock => ImGuiKey.ScrollLock,
            KeyboardKey.NumLock => ImGuiKey.NumLock,
            KeyboardKey.PrintScreen => ImGuiKey.PrintScreen,
            KeyboardKey.Pause => ImGuiKey.Pause,
            KeyboardKey.Keypad0 => ImGuiKey.Keypad0,
            KeyboardKey.Keypad1 => ImGuiKey.Keypad1,
            KeyboardKey.Keypad2 => ImGuiKey.Keypad2,
            KeyboardKey.Keypad3 => ImGuiKey.Keypad3,
            KeyboardKey.Keypad4 => ImGuiKey.Keypad4,
            KeyboardKey.Keypad5 => ImGuiKey.Keypad5,
            KeyboardKey.Keypad6 => ImGuiKey.Keypad6,
            KeyboardKey.Keypad7 => ImGuiKey.Keypad7,
            KeyboardKey.Keypad8 => ImGuiKey.Keypad8,
            KeyboardKey.Keypad9 => ImGuiKey.Keypad9,
            KeyboardKey.KeypadDecimal => ImGuiKey.KeypadDecimal,
            KeyboardKey.KeypadDivide => ImGuiKey.KeypadDivide,
            KeyboardKey.KeypadMultiply => ImGuiKey.KeypadMultiply,
            KeyboardKey.KeypadMinus => ImGuiKey.KeypadSubtract,
            KeyboardKey.KeypadPlus => ImGuiKey.KeypadAdd,
            KeyboardKey.KeypadEnter => ImGuiKey.KeypadEnter,
            KeyboardKey.ControlLeft => ImGuiKey.LeftCtrl,
            KeyboardKey.ShiftLeft => ImGuiKey.LeftShift,
            KeyboardKey.AltLeft => ImGuiKey.LeftAlt,
            KeyboardKey.WinLeft => ImGuiKey.LeftSuper,
            KeyboardKey.ControlRight => ImGuiKey.RightCtrl,
            KeyboardKey.ShiftRight => ImGuiKey.RightShift,
            KeyboardKey.AltRight => ImGuiKey.RightAlt,
            KeyboardKey.WinRight => ImGuiKey.RightSuper,
            KeyboardKey.Menu => ImGuiKey.Menu,
            KeyboardKey.Number0 => ImGuiKey.Key0,
            KeyboardKey.Number1 => ImGuiKey.Key1,
            KeyboardKey.Number2 => ImGuiKey.Key2,
            KeyboardKey.Number3 => ImGuiKey.Key3,
            KeyboardKey.Number4 => ImGuiKey.Key4,
            KeyboardKey.Number5 => ImGuiKey.Key5,
            KeyboardKey.Number6 => ImGuiKey.Key6,
            KeyboardKey.Number7 => ImGuiKey.Key7,
            KeyboardKey.Number8 => ImGuiKey.Key8,
            KeyboardKey.Number9 => ImGuiKey.Key9,
            KeyboardKey.A => ImGuiKey.A,
            KeyboardKey.B => ImGuiKey.B,
            KeyboardKey.C => ImGuiKey.C,
            KeyboardKey.D => ImGuiKey.D,
            KeyboardKey.E => ImGuiKey.E,
            KeyboardKey.F => ImGuiKey.F,
            KeyboardKey.G => ImGuiKey.G,
            KeyboardKey.H => ImGuiKey.H,
            KeyboardKey.I => ImGuiKey.I,
            KeyboardKey.J => ImGuiKey.J,
            KeyboardKey.K => ImGuiKey.K,
            KeyboardKey.L => ImGuiKey.L,
            KeyboardKey.M => ImGuiKey.M,
            KeyboardKey.N => ImGuiKey.N,
            KeyboardKey.O => ImGuiKey.O,
            KeyboardKey.P => ImGuiKey.P,
            KeyboardKey.Q => ImGuiKey.Q,
            KeyboardKey.R => ImGuiKey.R,
            KeyboardKey.S => ImGuiKey.S,
            KeyboardKey.T => ImGuiKey.T,
            KeyboardKey.U => ImGuiKey.U,
            KeyboardKey.V => ImGuiKey.V,
            KeyboardKey.W => ImGuiKey.W,
            KeyboardKey.X => ImGuiKey.X,
            KeyboardKey.Y => ImGuiKey.Y,
            KeyboardKey.Z => ImGuiKey.Z,
            KeyboardKey.F1 => ImGuiKey.F1,
            KeyboardKey.F2 => ImGuiKey.F2,
            KeyboardKey.F3 => ImGuiKey.F3,
            KeyboardKey.F4 => ImGuiKey.F4,
            KeyboardKey.F5 => ImGuiKey.F5,
            KeyboardKey.F6 => ImGuiKey.F6,
            KeyboardKey.F7 => ImGuiKey.F7,
            KeyboardKey.F8 => ImGuiKey.F8,
            KeyboardKey.F9 => ImGuiKey.F9,
            KeyboardKey.F10 => ImGuiKey.F10,
            KeyboardKey.F11 => ImGuiKey.F11,
            KeyboardKey.F12 => ImGuiKey.F12,
            KeyboardKey.F13 => ImGuiKey.F13,
            KeyboardKey.F14 => ImGuiKey.F14,
            KeyboardKey.F15 => ImGuiKey.F15,
            KeyboardKey.F16 => ImGuiKey.F16,
            KeyboardKey.F17 => ImGuiKey.F17,
            KeyboardKey.F18 => ImGuiKey.F18,
            KeyboardKey.F19 => ImGuiKey.F19,
            KeyboardKey.F20 => ImGuiKey.F20,
            KeyboardKey.F21 => ImGuiKey.F21,
            KeyboardKey.F22 => ImGuiKey.F22,
            KeyboardKey.F23 => ImGuiKey.F23,
            KeyboardKey.F24 => ImGuiKey.F24,
            _ => ImGuiKey.None
        };
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            ImGui.DestroyContext();
            
            if (this._textInputEnabled && Sdl.TextInputActive(this.Window.Handle)) {
                Sdl.StopTextInput(this.Window.Handle);
            }
            
            this._vertexBuffer.Dispose();
            this._indexBuffer.Dispose();
            this._projViewBuffer.Dispose();
            this._fontTexture.Dispose();
            this._effect.Dispose();
            
            this.Window.Resized -= this.OnWindowResized;
            this.Window.MouseMove -= this.OnMouseMove;
            this.Window.MouseWheel -= this.OnMouseWheel;
            this.Window.MouseButtonDown -= this.OnMouseButtonDown;
            this.Window.MouseButtonUp -= this.OnMouseButtonUp;
            this.Window.KeyDown -= this.OnKeyDown;
            this.Window.KeyUp -= this.OnKeyUp;
            this.Window.TextInput -= this.OnTextInput;
        }
    }
}
