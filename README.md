<p align="center" style="margin-bottom: 0px !important;">
  <img width="512" src="https://github.com/user-attachments/assets/cb8a5929-3f79-4a68-ab2c-36b395148c06" alt="Logo" align="center">
</p>

# Bliss 🚀
[![Discord](https://img.shields.io/discord/1199798541980283051?style=flat-square&logo=discord&label=Discord)](https://discord.gg/7XKw6YQa76)
[![License](https://img.shields.io/github/license/MrScautHD/Bliss?style=flat-square&logo=libreofficewriter&label=License)](LICENSE)
[![Activity](https://img.shields.io/github/commit-activity/w/MrScautHD/Bliss?style=flat-square&logo=Github&label=Activity)](https://github.com/MrScautHD/Bliss/activity)
[![Stars](https://img.shields.io/github/stars/MrScautHD/Bliss?style=flat-square&logo=Github&label=Stars)](https://github.com/MrScautHD/Bliss/stargazers)

__Bliss__ is a modern `Vulkan` Render Framework.

---

# 🪙 Installation - [Nuget](https://www.nuget.org/packages/Bliss)
```
Coming SoOn!
```

# 📖 [Installation - From source]
> 1. Clone this repository.
> 2. Add `Bliss.csproj` as a reference to your project.
> 3. Ensure that you downloaded the [`Vulkand SDK`](https://vulkan.lunarg.com/).
---

# ⚠️ Importand for the Installation
For this project, you need the [`Vulkan SDK`](https://vulkan.lunarg.com/sdk/home#windows) with the version `[1.3.283.0]`. Please add the following code to your `.csproj` file:
```cs
    <!-- Vulkan SDK -->
    <PropertyGroup>
        <VulkanBinPath>C:\VulkanSDK\1.3.283.0\Bin</VulkanBinPath>
    </PropertyGroup>
```

To **compile shaders**, include the following code in your `.csproj` file:
```cs
    <!-- Shader Stages (Vertex, Fragment...) -->
    <ItemGroup>
        <VertexShader Include="**/*.vert" />
        <FragmentShader Include="**/*.frag" />
    </ItemGroup>

    <!-- Compiled Shader Format -->
    <ItemGroup>
        <EmbeddedResource Include="**/*.spv" />
    </ItemGroup>
    
    <!-- Content -->
    <ItemGroup>
        <Content Include="content/**/*" Pack="true" Exclude="@(VertexShader);@(FragmentShader);content/**/*.spv">
            <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
            <PackageCopyToOutput>true</PackageCopyToOutput>
        </Content>
    </ItemGroup>

    <!-- Shader Cleaner -->
    <Target Name="CleanVulkanShader" BeforeTargets="Clean">
        <Message Text="......................................SHADERS.Clean..........................................................." Importance="high" />
        <ItemGroup>
            <FilesToDelete Include="**\*.spv" />
        </ItemGroup>
        <Delete Files="@(FilesToDelete)" />
        <Message Text="......................................SHADERS.Cleaned........................................................." Importance="high" />
    </Target>

    <!-- Shader Compiler -->
    <Target Name="BuildVulkanShader" BeforeTargets="BeforeBuild">
        <Message Text="......................................SHADERS.Compile........................................................." Importance="high" />
        <Message Text="   Starting Vulkan Shader Compilation..." Importance="high" />
        <Message Text="     VulkanBinPath: $(VulkanBinPath)" Importance="high" />
        <Message Text="     VertexShader: @(VertexShader)" Importance="high" />
        <Message Text="     FragmentShader: @(FragmentShader)" Importance="high" />
        <Exec Command="$(VulkanBinPath)\glslc.exe &quot;%(VertexShader.FullPath)&quot; -o &quot;%(VertexShader.FullPath).spv&quot;" Condition="'@(VertexShader)'!=''" />
        <Exec Command="$(VulkanBinPath)\glslc.exe &quot;%(FragmentShader.FullPath)&quot; -o &quot;%(FragmentShader.FullPath).spv&quot;" Condition="'@(FragmentShader)'!=''" />
        <Message Text="......................................SHADERS.Compiled........................................................" Importance="high" />
    </Target>
```

# 💻 Platforms
[<img src="https://github.com/MrScautHD/Sparkle/assets/65916181/a92bd5fa-517b-44c2-ab58-cc01b5ae5751" alt="windows" width="70" height="70" align="left">](https://www.microsoft.com/de-at/windows)
### Windows
=======

[<img src="https://github.com/MrScautHD/Sparkle/assets/65916181/f9e643a8-4d46-450c-91ac-d220394ecd42" alt="Linux" width="70" height="70" align="left">](https://www.ubuntu.com/)
### Linux
=======

[<img src="https://github.com/MrScautHD/Sparkle/assets/65916181/e37eb15f-4237-47ae-9ae7-e4455f7c3d92" alt="macOS" width="70" height="70" align="left">](https://www.apple.com/at/macos/sonoma/)
### MacOS
=======

# 🧑 Contributors
<a href="https://github.com/mrscauthd/Bliss/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=mrscauthd/Bliss&max=500&columns=20&anon=1" />
</a>

# ✉️ Reach us
[<img src="https://github.com/MrScautHD/Sparkle/assets/65916181/87b291cd-6506-4fb5-b032-abf3170a28c4" alt="discord" width="186" height="60">](https://discord.gg/7XKw6YQa76)
[<img src="https://github.com/MrScautHD/Sparkle/assets/65916181/de09f016-db11-4554-aa56-4d1bd6c2464f" alt="sponsor" width="186" height="60">](https://github.com/sponsors/MrScautHD)

---

# ✍️ Acknowledgement
This library is available under the [MIT](https://choosealicense.com/licenses/mit) license.

Special thanks to the author(s) and contributors of the following projects
* [Silk.NET](https://github.com/dotnet/Silk.NET)
* [SilkVulkanTutorial](https://github.com/stymee/SilkVulkanTutorial)
