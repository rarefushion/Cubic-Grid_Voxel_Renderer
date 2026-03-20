using System.Runtime.InteropServices;
using Silk.NET.GLFW;
using StbImageSharp;

namespace GalensUnified.CubicGrid.Renderer.NET;

public static class TextureLoader
{

    /// <summary>Loads a collection of images from the specified file information array into a dictionary.</summary>
    /// <param name="images">An array of <see cref="FileInfo"/> objects representing the image files to load.</param>
    /// <param name="flip">Whether to flip the images during loading. Defaults to <c>true</c>.</param>
    /// <returns>A dictionary where the key is the filename without extension and the value is the loaded <see cref="Image"/>.</returns>
    public static Dictionary<string, Image> LoadImages(FileInfo[] images, bool flip = true)
    {
        Dictionary<string, Image> toReturn = [];
        foreach (FileInfo texture in images.OrderBy(di => di.Name))
        {
            Image image = LoadImage(texture, flip);
            toReturn.Add(new(Path.GetFileNameWithoutExtension(texture.Name)), image);
        }
        return toReturn;
    }

    /// <summary>Loads a single image from a file into unmanaged memory.</summary>
    /// <param name="path">The <see cref="FileInfo"/> pointing to the image file.</param>
    /// <param name="flip">Whether to flip the image vertically. Defaults to <c>true</c>.</param>
    /// <returns>An <see cref="Image"/> object containing dimensions and a pointer to the pixel data.</returns>
    /// <remarks>This method allocates unmanaged memory via <see cref="Marshal.AllocHGlobal"/>.</remarks>
    public unsafe static Image LoadImage(FileInfo path, bool flip = true)
    {
        Image toReturn;
        using (Stream stream = path.OpenRead())
        {
            ImageResult imageResult = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            toReturn = new Image
                    {
                        Width = imageResult.Width,
                        Height = imageResult.Height,
                        // Pin the byte array in memory and get a pointer to the raw pixel data
                        Pixels = (byte*)Marshal.AllocHGlobal(imageResult.Data.Length)
                    };

            // Copy the data to the unmanaged memory
            Marshal.Copy(imageResult.Data, 0, (IntPtr)toReturn.Pixels, imageResult.Data.Length);
        }
        if (flip)
            return FlipImage(toReturn);
        else
            return toReturn;
    }

    /// <summary>Flips the pixel data of an image.</summary>
    /// <param name="image">The <see cref="Image"/> to flip.</param>
    /// <returns>The <see cref="Image"/> with updated pixel data pointers.</returns>
    /// <warning>Note: This method currently assigns a pointer to a local stack-allocated array, which may cause memory safety issues outside the scope of the method.</warning>
    public unsafe static Image FlipImage(Image image)
    {
        int count = image.Height * image.Width;
        byte[] fliped = new byte[count * 4];
        for (int p = 0; p < count; p++)
        {
            int index = p * 4;
            fliped[fliped.Length - 1 - (index + 0)] = image.Pixels[index + 3];
            fliped[fliped.Length - 1 - (index + 1)] = image.Pixels[index + 2];
            fliped[fliped.Length - 1 - (index + 2)] = image.Pixels[index + 1];
            fliped[fliped.Length - 1 - (index + 3)] = image.Pixels[index + 0];
        }

        fixed (byte* ptr = fliped)
        {
            image.Pixels = ptr;
        }
        return image;
    }
}