using AwesomeAssertions;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

/// <summary>
/// Regression tests for the G3.2 finding that the polymorphic discriminator was emitted only
/// for the top-level declared type — a derived value in a nested property (or a collection
/// element) was serialised without its discriminator, silently losing the concrete type on
/// the deserialise round-trip.
/// </summary>
public class PolymorphicNestedTests
{
    [HumlPolymorphic("kind")]
    [HumlDerivedType(typeof(Dog), "dog")]
    [HumlDerivedType(typeof(Cat), "cat")]
    public abstract class Animal
    {
        public string Name { get; set; } = "";
    }

    public sealed class Dog : Animal
    {
        public bool GoodBoy { get; set; }
    }

    public sealed class Cat : Animal
    {
        public int Lives { get; set; }
    }

    public sealed class Household
    {
        public Animal Pet { get; set; } = new Dog();
        public IList<Animal> Animals { get; set; } = [];
    }

    [Fact]
    public void Nested_polymorphic_property_round_trips_concrete_type()
    {
        var house = new Household { Pet = new Cat { Name = "Felix", Lives = 9 } };

        var huml = HumlSerializer.Serialize(house);
        var restored = HumlSerializer.Deserialize<Household>(huml, HumlOptions.Default);

        restored.Pet.Should().BeOfType<Cat>();
        ((Cat)restored.Pet).Lives.Should().Be(9);
        restored.Pet.Name.Should().Be("Felix");
    }

    [Fact]
    public void Polymorphic_collection_elements_round_trip_concrete_types()
    {
        var house = new Household
        {
            Animals = [new Dog { Name = "Rex", GoodBoy = true }, new Cat { Name = "Tom", Lives = 7 }],
        };

        var huml = HumlSerializer.Serialize(house);
        var restored = HumlSerializer.Deserialize<Household>(huml, HumlOptions.Default);

        restored.Animals.Should().HaveCount(2);
        restored.Animals[0].Should().BeOfType<Dog>();
        restored.Animals[1].Should().BeOfType<Cat>();
        ((Cat)restored.Animals[1]).Lives.Should().Be(7);
    }
}
