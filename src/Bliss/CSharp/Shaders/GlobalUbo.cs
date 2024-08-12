using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bliss.CSharp.Shaders;

public class GlobalUbo
{
    private Matrix4x4 projection;       // 64
    private Matrix4x4 view;             // 64
    private Vector4 frontVec;           // 16
    private Vector4 ambientColor;       // 16
    private int numLights;              // 4
    private int padding1;               // 4
    private int padding2;               // 4
    private int padding3;               // 4
    private PointLight[] pointLights = null!;   // 10 * 32 * 320
    // total size = 496

    public GlobalUbo()
    {
        projection = Matrix4x4.Identity;
        view = Matrix4x4.Identity;
        frontVec = Vector4.UnitZ;
        ambientColor = new(1f, 1f, 1f, 0.02f);
        numLights = 0;
        padding1 = 0;
        padding2 = 0;
        padding3 = 0;
        pointLights = new PointLight[10];
    }
    
    public void Update(Matrix4x4 projection, Matrix4x4 view, Vector4 frontVec)
    {
        this.projection = projection;
        this.view = view;
        this.frontVec = frontVec;
    }


    public void SetNumLights(int numLights)
    {
        this.numLights = numLights;
    }

    public void SetPointLightTranslation(int lightIndex, Vector3 translation)
    {
        pointLights[lightIndex].SetPosition(translation);
    }

    public void SetPointLightColor(int lightIndex, Vector4 color, float intensity)
    {
        pointLights[lightIndex].SetColor(color, intensity);
    }

    public byte[] AsBytes()
    {
        uint offset = 0;
        uint fsize = sizeof(float);
        uint vsize = fsize * 4;
        uint msize = vsize * 4;
        var bytes = new byte[SizeOf()];

        projection.AsBytes().CopyTo(bytes, offset);
        offset += msize;
        view.AsBytes().CopyTo(bytes, offset);
        offset += msize;

        frontVec.AsBytes().CopyTo(bytes, offset);
        offset += vsize;
        ambientColor.AsBytes().CopyTo(bytes, offset);
        offset += vsize;

        numLights.AsBytes().CopyTo(bytes, offset);
        offset += fsize;
        padding1.AsBytes().CopyTo(bytes, offset);
        offset += fsize;
        padding2.AsBytes().CopyTo(bytes, offset);
        offset += fsize;
        padding3.AsBytes().CopyTo(bytes, offset);
        offset += fsize;

        var pbytes = pointLights.AsBytes();
        pbytes.CopyTo(bytes, offset);

        return bytes;
    }

    public GlobalUbo(byte[] bytes)
    {
        int offset = 0;
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                projection[row, col] = BitConverter.ToSingle(bytes[offset..(offset + 4)]);
                offset += 4;
            }
        }
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                view[row, col] = BitConverter.ToSingle(bytes[offset..(offset + 4)]);
                offset += 4;
            }
        }
        for (int col = 0; col < 4; col++)
        {
            frontVec[col] = BitConverter.ToSingle(bytes[offset..(offset + 4)]);
            offset += 4;
        }
        for (int col = 0; col < 4; col++)
        {
            ambientColor[col] = BitConverter.ToSingle(bytes[offset..(offset + 4)]);
            offset += 4;
        }
        numLights = BitConverter.ToInt32(bytes[offset..(offset + 4)]);
        offset += 4;
        padding1 = BitConverter.ToInt32(bytes[offset..(offset + 4)]);
        offset += 4;
        padding2 = BitConverter.ToInt32(bytes[offset..(offset + 4)]);
        offset += 4;
        padding3 = BitConverter.ToInt32(bytes[offset..(offset + 4)]);
        offset += 4;

        for (int pt = 0; pt < 10; pt++)
        {
            var ptpos = Vector4.Zero;
            for (int col = 0; col < 4; col++)
            {
                ptpos[col] = BitConverter.ToSingle(bytes[offset..(offset + 4)]);
                offset += 4;
            }
            var ptcol = Vector4.One;
            for (int col = 0; col < 4; col++)
            {
                ptcol[col] = BitConverter.ToSingle(bytes[offset..(offset + 4)]);
                offset += 4;
            }
            pointLights[pt] = new PointLight(ptpos, ptcol);
        }

    }

    public static uint SizeOf() => 496;// (uint)Unsafe.SizeOf<GlobalUbo2>();

}


public struct PointLight
{
    private Vector4 position = Vector4.Zero;
    private Vector4 color = Vector4.One;

    public PointLight()
    {

    }
    public PointLight(Vector4 position, Vector4 color)
    {
        this.position = position;
        this.color = color;
    }
    
    public void SetPosition(Vector3 pos)
    {
        position = new Vector4(pos.X, pos.Y, pos.Z, 0f);
    }
    public void SetColor(Vector4 col, float intensity)
    {
        color = new Vector4(col.X, col.Y, col.Z, intensity);
    }

    public byte[] AsBytes()
    {
        var bytes = new byte[32];
        position.AsBytes().CopyTo(bytes, 0);
        color.AsBytes().CopyTo(bytes, 16);

        return bytes;
    }

    public static uint SizeOf() => (uint)Unsafe.SizeOf<PointLight>();

    public override string ToString()
    {
        return $"p:{position}, c:{color}";
    }
}



public static class TypeExtensions
{
    public static byte[] AsBytes(this int i)
    {
        var bytes = new byte[4];
        BitConverter.GetBytes(i).CopyTo(bytes, 0);
        return bytes;
    }

    public static byte[] AsBytes(this float f)
    {
        var bytes = new byte[4];
        BitConverter.GetBytes(f).CopyTo(bytes, 0);
        return bytes;
    }

    public static byte[] AsBytes(this Vector4 vec)
    {
        uint offset = 0;
        uint fsize = 4;
        var bytes = new byte[16];
        BitConverter.GetBytes(vec.X).CopyTo(bytes, offset);
        BitConverter.GetBytes(vec.Y).CopyTo(bytes, offset += fsize);
        BitConverter.GetBytes(vec.Z).CopyTo(bytes, offset += fsize);
        BitConverter.GetBytes(vec.W).CopyTo(bytes, offset += fsize);

        return bytes;
    }

    public static byte[] AsBytes(this Matrix4x4 mat)
    {
        uint offset = 0;
        uint fsize = 4;
        var bytes = new byte[64];
        for (int row=0; row<4; row++)
        {
            for(int col=0; col<4; col++)
            {
                BitConverter.GetBytes((float)mat[row, col]).CopyTo(bytes, offset);
                offset += fsize;
            }
        }
        return bytes;
    }


    public static byte[] AsBytes(this PointLight[] pts)
    {
        uint offset = 0;
        //uint psize = 32;
        var bytes = new byte[320];
        for (uint i = 0; i < 10; i++)
        {
            pts[i].AsBytes().CopyTo(bytes, offset);
            offset += 32;
        }

        return bytes;
    }
}