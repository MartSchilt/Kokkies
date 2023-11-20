using Godot;
using System;

namespace Kokkies;

/// <summary>
/// Static class that gives access to some nice to have methods
/// </summary>
public static class Helper
{
    public static Color RandomColor()
    {
        Random rnd = new Random();
        byte[] b = new byte[3];
        rnd.NextBytes(b);
        return Color.Color8(b[0], b[1], b[2]);
    }
}
