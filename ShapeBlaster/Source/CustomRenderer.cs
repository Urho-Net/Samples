

// Custom Renderer

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Urho;
using Urho.Urho2D;

namespace ShapeBlaster
{


    [StructLayout(LayoutKind.Sequential)]
    struct PositionColorUVVertex
    {
        public float X, Y, Z;
        public uint Color;
        public float U, V;
    };

    class Batch
    {
        public Texture2D Texture;
        public uint VertexCount = 0;
        public PositionColorUVVertex[] Vertices = new PositionColorUVVertex[256];
    }

    struct DrawItem
    {
        public Texture2D Texture;
        public uint StartVertex;
        public uint VertexCount;
    }

    static class CustomRenderer
    {
        static StringHash VSP_MODEL = new StringHash("Model");
        static StringHash PSP_MATDIFFCOLOR = new StringHash("MatDiffColor");

        // 250k max vertices
        const uint maxVertices = 256 * 1024;

        static uint totalVertex = 0;

        static VertexBuffer vertexBuffer;

        static ShaderVariation pixelShader;
        static ShaderVariation vertexShader;

        static SortedDictionary<float, Dictionary<Texture2D, Batch>> layerBatches = new SortedDictionary<float, Dictionary<Texture2D, Batch>>();

        public static void Initialize()
        {

            var graphics = Application.Current.Graphics;

            pixelShader = graphics.GetShader(ShaderType.PS, "ShapeBlaster");
            vertexShader = graphics.GetShader(ShaderType.VS, "ShapeBlaster");

            vertexBuffer = new VertexBuffer(Application.CurrentContext);
            vertexBuffer.SetSize(maxVertices, ElementMask.Position | ElementMask.Color | ElementMask.TexCoord1, true);

        }

        public static void Begin()
        {
            totalVertex = 0;

            // reset batches
            foreach (var layer in layerBatches)
            {
                foreach (var batch in layer.Value.Values)
                {
                    batch.VertexCount = 0;

                }
            }

        }


        public static void End()
        {

            List<DrawItem> drawList = new List<DrawItem>();


            if (totalVertex == 0)
                return;

            IntPtr vertexDataPtr = vertexBuffer.Lock(0, totalVertex, true);

            if (vertexDataPtr == IntPtr.Zero)
                return;


            uint startVertex = 0;
            uint vertexSize = vertexBuffer.VertexSize;

            foreach (var layer in layerBatches)
            {
                foreach (var batch in layer.Value.Values)
                {
                    if (totalVertex + batch.VertexCount >= maxVertices)
                    {
                        throw new System.InvalidOperationException("Ran out of vertices");
                    }

                    if (batch.VertexCount == 0)
                        continue;

                    if (Application.Platform == Platforms.Web)
                    {
                        // Looks like this one is faster on Web 
                        byte[] bytes = ToByteArray(batch.Vertices);
                        Marshal.Copy(bytes, 0, vertexDataPtr, (int)(vertexSize * batch.VertexCount));
                        vertexDataPtr = IntPtr.Add(vertexDataPtr, (int)(vertexSize * batch.VertexCount));
                    }
                    else
                    {
                        // faster blit possible?
                        for (int i = 0; i < batch.VertexCount; i++)
                        {
                            Marshal.StructureToPtr(batch.Vertices[i], vertexDataPtr, true);
                            vertexDataPtr = IntPtr.Add(vertexDataPtr, (int)vertexSize);
                        }
                    }

                    var item = new DrawItem();
                    item.Texture = batch.Texture;
                    item.StartVertex = startVertex;
                    item.VertexCount = batch.VertexCount;

                    startVertex += batch.VertexCount;
                    drawList.Add(item);

                }

            }


            vertexBuffer.Unlock();

            var renderer = Application.Current.Renderer;
            var graphics = Application.Current.Graphics;

            var view = renderer.GetViewport(0).View;
            var camera = renderer.GetViewport(0).Camera;

            if (view == null || camera == null)
                return;

            graphics.SetBlendMode(BlendMode.Addalpha);
            graphics.CullMode = CullMode.None;
            graphics.FillMode = FillMode.Solid;
            graphics.DepthTest = CompareMode.Always;


            graphics.SetShaders(vertexShader, pixelShader);

            view.SetCameraShaderParameters(camera);
            graphics.SetShaderParameter(VSP_MODEL, Matrix3x4.Identity);

            graphics.SetShaderParameter(PSP_MATDIFFCOLOR, Color.White);

            graphics.SetVertexBuffer(vertexBuffer);

            foreach (var item in drawList)
            {
                graphics.SetTexture((int)TextureUnit.Diffuse, item.Texture);
                graphics.Draw(PrimitiveType.TriangleList, item.StartVertex, item.VertexCount);
            }

            graphics.SetTexture(0, null);


        }

        public static void Draw(CustomSprite texture, Vector2 position, Color color, float rotation, Vector2 origin, float scale, float layerDepth)
        {

            var w = texture.Width * scale;
            var h = texture.Height * scale;

            DrawInternal(texture,
                new Vector4(position.X, position.Y, w, h),
                color,
                rotation,
                origin * scale,
                layerDepth);
        }

        public static void Draw(CustomSprite texture, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, float layerDepth)
        {

            var w = texture.Width * scale.X;
            var h = texture.Height * scale.Y;

            DrawInternal(texture,
                new Vector4(position.X, position.Y, w, h),
                color,
                rotation,
                origin * scale,
                layerDepth);
        }

        public static void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 2f)
        {
            Vector2 delta = end - start;
            Draw(Art.Pixel, start, color, delta.ToAngle(), new Vector2(0, 0.5f), new Vector2(delta.Length, thickness), 0f);
        }


        static void DrawInternal(CustomSprite sprite, Vector4 destinationRectangle, Color color, float rotation, Vector2 origin, float depth)
        {
            Dictionary<Texture2D, Batch> batches;

            if (!layerBatches.TryGetValue(depth, out batches))
            {
                batches = new Dictionary<Texture2D, Batch>();
                layerBatches[depth] = batches;
            }

            Batch batch;

            var texture = sprite.Texture;

            if (!batches.TryGetValue(texture, out batch))
            {
                batch = new Batch();
                batch.Texture = texture;
                batches[texture] = batch;
            }

            if (totalVertex + 6 >= maxVertices)
            {
                throw new System.InvalidOperationException("Ran out of vertices");
            }

            totalVertex += 6;

            if (batch.VertexCount + 6 >= batch.Vertices.Length)
            {
                Array.Resize(ref batch.Vertices, batch.Vertices.Length * 2);
            }

            if (rotation == 0f)
            {
                Set(batch.Vertices, ref batch.VertexCount, destinationRectangle.X - origin.X,
                        destinationRectangle.Y - origin.Y,
                        destinationRectangle.Z,
                        destinationRectangle.W,
                        color,
                        sprite.TexCoordTL,
                        sprite.TexCoordBR,
                        depth);

            }
            else
            {
                Set(batch.Vertices, ref batch.VertexCount, destinationRectangle.X,
                        destinationRectangle.Y,
                        -origin.X,
                        -origin.Y,
                        destinationRectangle.Z,
                        destinationRectangle.W,
                        (float)Math.Sin(rotation),
                        (float)Math.Cos(rotation),
                        color,
                        sprite.TexCoordTL,
                        sprite.TexCoordBR,
                        depth);

            }

        }


        static Vector2 _texCoordTL = new Vector2(0, 0);
        static Vector2 _texCoordBR = new Vector2(1, 1);

        static PositionColorUVVertex vertexTL = new PositionColorUVVertex();
        static PositionColorUVVertex vertexTR = new PositionColorUVVertex();
        static PositionColorUVVertex vertexBL = new PositionColorUVVertex();
        static PositionColorUVVertex vertexBR = new PositionColorUVVertex();

        // Portions Copyright (C) The MonoGame Team
        static public void Set(PositionColorUVVertex[] vertices, ref uint vertexCount, float x, float y, float dx, float dy, float w, float h, float sin, float cos, Color color, Vector2 texCoordTL, Vector2 texCoordBR, float depth)
        {
            uint ucolor = color.ToUInt();

            vertexTL.X = x + dx * cos - dy * sin;
            vertexTL.Y = y + dx * sin + dy * cos;
            vertexTL.Z = depth;
            vertexTL.Color = ucolor;
            vertexTL.U = texCoordTL.X;
            vertexTL.V = texCoordTL.Y;

            vertexTR.X = x + (dx + w) * cos - dy * sin;
            vertexTR.Y = y + (dx + w) * sin + dy * cos;
            vertexTR.Z = depth;
            vertexTR.Color = ucolor;
            vertexTR.U = texCoordBR.X;
            vertexTR.V = texCoordTL.Y;

            vertexBL.X = x + dx * cos - (dy + h) * sin;
            vertexBL.Y = y + dx * sin + (dy + h) * cos;
            vertexBL.Z = depth;
            vertexBL.Color = ucolor;
            vertexBL.U = texCoordTL.X;
            vertexBL.V = texCoordBR.Y;

            vertexBR.X = x + (dx + w) * cos - (dy + h) * sin;
            vertexBR.Y = y + (dx + w) * sin + (dy + h) * cos;
            vertexBR.Z = depth;
            vertexBR.Color = ucolor;
            vertexBR.U = texCoordBR.X;
            vertexBR.V = texCoordBR.Y;

            vertices[vertexCount++] = vertexTL;
            vertices[vertexCount++] = vertexTR;
            vertices[vertexCount++] = vertexBL;

            vertices[vertexCount++] = vertexTR;
            vertices[vertexCount++] = vertexBR;
            vertices[vertexCount++] = vertexBL;

        }

        static public void Set(PositionColorUVVertex[] vertices, ref uint vertexCount, float x, float y, float w, float h, Color color, Vector2 texCoordTL, Vector2 texCoordBR, float depth)
        {
            uint ucolor = color.ToUInt();

            vertexTL.X = x;
            vertexTL.Y = y;
            vertexTL.Z = depth;
            vertexTL.Color = ucolor;
            vertexTL.U = texCoordTL.X;
            vertexTL.V = texCoordTL.Y;

            vertexTR.X = x + w;
            vertexTR.Y = y;
            vertexTR.Z = depth;
            vertexTR.Color = ucolor;
            vertexTR.U = texCoordBR.X;
            vertexTR.V = texCoordTL.Y;

            vertexBL.X = x;
            vertexBL.Y = y + h;
            vertexBL.Z = depth;
            vertexBL.Color = ucolor;
            vertexBL.U = texCoordTL.X;
            vertexBL.V = texCoordBR.Y;

            vertexBR.X = x + w;
            vertexBR.Y = y + h;
            vertexBR.Z = depth;
            vertexBR.Color = ucolor;
            vertexBR.U = texCoordBR.X;
            vertexBR.V = texCoordBR.Y;

            vertices[vertexCount++] = vertexTL;
            vertices[vertexCount++] = vertexTR;
            vertices[vertexCount++] = vertexBL;

            vertices[vertexCount++] = vertexTR;
            vertices[vertexCount++] = vertexBR;
            vertices[vertexCount++] = vertexBL;

        }


        private static byte[] ToByteArray<T>(T[] source) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(source, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                byte[] destination = new byte[source.Length * Marshal.SizeOf(typeof(T))];
                Marshal.Copy(pointer, destination, 0, destination.Length);
                return destination;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }

        private static T[] FromByteArray<T>(byte[] source) where T : struct
        {
            T[] destination = new T[source.Length / Marshal.SizeOf(typeof(T))];
            GCHandle handle = GCHandle.Alloc(destination, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                Marshal.Copy(source, 0, pointer, source.Length);
                return destination;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }

    }
}