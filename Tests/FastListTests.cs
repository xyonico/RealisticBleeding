using RealisticBleeding;

namespace Tests;

public class FastListTests
{
    [Test]
    public void NoAllocAdd_Works()
    {
        var list = new FastList<int>(4);

        for (var i = 0; i < 4; i++)
        {
            Assert.That(list.Count, Is.EqualTo(i));
            Assert.That(list.TryAddNoResize(i), Is.True);
        }

        Assert.That(list.Count, Is.EqualTo(4));

        Assert.That(list.TryAddNoResize(5), Is.False);
    }

    [Test]
    public void Add_Works()
    {
        var list = new FastList<int>(4);

        Assert.That(list.Count, Is.EqualTo(0));

        for (var i = 0; i < 8; i++)
        {
            list.Add(i);
            Assert.That(list.Count, Is.EqualTo(i + 1));
        }

        Assert.That(list.Capacity, Is.GreaterThan(4));
    }

    [Test]
    public void Insert_Works()
    {
        var list = new FastList<int>(4);

        for (var i = 0; i < 4; i++)
        {
            list.Add(i);
        }

        Assert.That(list.Count, Is.EqualTo(4));
        Assert.That(list.Capacity, Is.EqualTo(4));

        list.Insert(1, -1);

        Assert.Multiple(() =>
        {
            Assert.That(list.Count, Is.EqualTo(5));
            Assert.That(list.Capacity, Is.GreaterThan(4));
            Assert.That(list[1], Is.EqualTo(-1));
        });
    }
}