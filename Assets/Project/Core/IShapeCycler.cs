//A shape that can switch between several versions of itself, e.g. the cat's animations or the different balloons.
//The cycle button shows CurrentName and calls Next().
public interface IShapeCycler
{
    //How many versions there are to switch between.
    int Count { get; }

    string CurrentName { get; }

    void Next();
}
