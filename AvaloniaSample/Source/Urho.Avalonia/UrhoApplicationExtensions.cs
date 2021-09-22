using Avalonia;
using Avalonia.Controls;

namespace Urho.Avalonia
{
    public static class UrhoApplicationExtensions
    {
        /// <summary>
        ///     Enable Skia renderer.
        /// </summary>
        /// <typeparam name="T">Builder type.</typeparam>
        /// <param name="builder">Builder.</param>
        /// <returns>Configure builder.</returns>
        public static T UsePortablePlatfrom<T>(this T builder, AvaloniaUrhoContext context) where T : AppBuilderBase<T>, new()
        {
            return builder.UseWindowingSubsystem(
                () => PortableWindowPlatform.Initialize(context),
                "PortableUrho3DPlatform");
        }
    }
}