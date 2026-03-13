namespace FoodShelves;

public interface ITransformationException {
    void Apply(BEBaseFSContainer be, TransformationData t);
}