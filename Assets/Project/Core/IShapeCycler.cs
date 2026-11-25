//A shape that can switch between several versions of itself, e.g. the lion's animations or the different balloons.
//The variant dropdown lists the names and calls Select().
public interface IShapeCycler
{
    //How many versions there are to switch between.
    int Count { get; }

    int CurrentIndex { get; }

    string CurrentName { get; }

    string NameAt(int index);

    void Select(int index);
}
