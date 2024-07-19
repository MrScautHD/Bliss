using Bliss.CSharp.Windowing;
using Silk.NET.Maths;

using BlissWindow window = new BlissWindow(new Vector2D<int>(1270, 720), "TEST");
window.Init();

window.Move += (vector2D => {
    Console.WriteLine("TESTST"); 
});