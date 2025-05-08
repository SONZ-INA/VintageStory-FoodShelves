namespace FoodShelves;

public class UniversalShapeTextureSource : ITexPositionSource {
    private readonly ICoreClientAPI capi;
    private readonly ITextureAtlasAPI targetAtlas;
    private readonly Shape shape;
    private readonly string filenameForLogging;
    public Dictionary<string, CompositeTexture> textures = new();
    public TextureAtlasPosition firstTexPos;

    private HashSet<AssetLocation> missingTextures = new();

    public UniversalShapeTextureSource(ICoreClientAPI capi, ITextureAtlasAPI targetAtlas, Shape shape, string filenameForLogging) {
        this.capi = capi;
        this.targetAtlas = targetAtlas;
        this.shape = shape;
        this.filenameForLogging = filenameForLogging;
    }

    public TextureAtlasPosition this[string textureCode] {
        get {
            TextureAtlasPosition texPos;

            if (textures.TryGetValue(textureCode, out var ctex)) {
                targetAtlas.GetOrInsertTexture(ctex, out _, out texPos);
            }
            else {
                shape.Textures.TryGetValue(textureCode, out var texturePath);

                if (texturePath == null) {
                    if (!missingTextures.Contains(texturePath)) {
                        capi.Logger.Warning("Shape {0} has an element using texture code {1}, but no such texture exists", filenameForLogging, textureCode);
                        missingTextures.Add(texturePath);
                    }

                    return targetAtlas.UnknownTexturePosition;
                }

                targetAtlas.GetOrInsertTexture(texturePath, out _, out texPos);
            }

            if (texPos == null) return targetAtlas.UnknownTexturePosition;
            firstTexPos ??= texPos;

            return texPos;
        }
    }

    public Size2i AtlasSize => targetAtlas.Size;
}