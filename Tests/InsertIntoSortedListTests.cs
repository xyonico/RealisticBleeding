using RealisticBleeding;

namespace Tests;

public class InsertIntoSortedListTests
{
    [Test]
    public void InsertingRandomSequence_BecomesSorted()
    {
        var list = new FastList<int>(32);

        var random = new Random();

        for (var i = 0; i < list.Capacity; i++)
        {
            list.InsertIntoSortedList(random.Next());
        }

        var prevValue = int.MinValue;

        for (var i = 0; i < list.Capacity; i++)
        {
            var value = list[i];
            
            Assert.That(value, Is.GreaterThanOrEqualTo(prevValue));

            prevValue = value;
        }
    }
}