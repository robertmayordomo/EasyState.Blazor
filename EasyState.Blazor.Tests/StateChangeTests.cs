namespace EasyState.Blazor.Tests;

public class StateChangeTests
{
    public class TestState
    {
        public string Name { get; set; } = string.Empty;
        public int Counter { get; set; }
        public bool IsActive { get; set; }
    }

    [Fact]
    public void PropertyChange_Constructor_SetsProperties()
    {
        var propertyChange = new PropertyChange("Name", "OldValue", "NewValue");

        Assert.Equal("Name", propertyChange.PropertyName);
        Assert.Equal("OldValue", propertyChange.OldValue);
        Assert.Equal("NewValue", propertyChange.NewValue);
    }

    [Fact]
    public void PropertyChange_WithNullValues_HandlesGracefully()
    {
        var propertyChange = new PropertyChange("Name", null, null);

        Assert.Equal("Name", propertyChange.PropertyName);
        Assert.Null(propertyChange.OldValue);
        Assert.Null(propertyChange.NewValue);
    }

    [Fact]
    public void PropertyChange_WithDifferentTypes_StoresCorrectly()
    {
        var stringChange = new PropertyChange("Name", "Old", "New");
        var intChange = new PropertyChange("Counter", 10, 20);
        var boolChange = new PropertyChange("IsActive", false, true);

        Assert.IsType<string>(stringChange.OldValue);
        Assert.IsType<string>(stringChange.NewValue);
        Assert.IsType<int>(intChange.OldValue);
        Assert.IsType<int>(intChange.NewValue);
        Assert.IsType<bool>(boolChange.OldValue);
        Assert.IsType<bool>(boolChange.NewValue);
    }

    [Fact]
    public void StateChange_Constructor_SetsProperties()
    {
        var state = new TestState { Name = "Test", Counter = 42 };
        var changes = new List<PropertyChange>
        {
            new PropertyChange("Name", "Old", "Test"),
            new PropertyChange("Counter", 0, 42)
        };

        var stateChange = new StateChange<TestState>(state, changes);

        Assert.Same(state, stateChange.State);
        Assert.Equal(2, stateChange.ChangedProperties.Count);
        Assert.Contains(stateChange.ChangedProperties, c => c.PropertyName == "Name");
        Assert.Contains(stateChange.ChangedProperties, c => c.PropertyName == "Counter");
    }

    [Fact]
    public void StateChange_WithEmptyChanges_ReturnsEmptyList()
    {
        var state = new TestState();
        var changes = new List<PropertyChange>();

        var stateChange = new StateChange<TestState>(state, changes);

        Assert.Empty(stateChange.ChangedProperties);
    }

    [Fact]
    public void StateChange_ChangedProperties_IsReadOnly()
    {
        var state = new TestState();
        var changes = new List<PropertyChange>
        {
            new PropertyChange("Name", "Old", "New")
        };

        var stateChange = new StateChange<TestState>(state, changes);

        Assert.IsAssignableFrom<IReadOnlyList<PropertyChange>>(stateChange.ChangedProperties);
    }

    [Fact]
    public void StateChange_ContainsCorrectStateReference()
    {
        var state = new TestState { Name = "Current", Counter = 100 };
        var changes = new List<PropertyChange>
        {
            new PropertyChange("Counter", 50, 100)
        };

        var stateChange = new StateChange<TestState>(state, changes);

        Assert.Equal("Current", stateChange.State.Name);
        Assert.Equal(100, stateChange.State.Counter);
    }

    [Fact]
    public void PropertyChange_EqualityComparison_WorksCorrectly()
    {
        var change1 = new PropertyChange("Name", "Old", "New");
        var change2 = new PropertyChange("Name", "Old", "New");

        Assert.Equal(change1.PropertyName, change2.PropertyName);
        Assert.Equal(change1.OldValue, change2.OldValue);
        Assert.Equal(change1.NewValue, change2.NewValue);
    }

    [Fact]
    public void StateChange_MultipleChangesToSameProperty_LastChangeWins()
    {
        var state = new TestState { Name = "Final" };
        var changes = new List<PropertyChange>
        {
            new PropertyChange("Name", "First", "Second"),
            new PropertyChange("Name", "Second", "Final")
        };

        var stateChange = new StateChange<TestState>(state, changes);

        Assert.Equal(2, stateChange.ChangedProperties.Count);
        var lastChange = stateChange.ChangedProperties[1];
        Assert.Equal("Final", lastChange.NewValue);
    }

    [Fact]
    public void StateChange_WithComplexObjects_StoresReferences()
    {
        var oldObject = new TestState { Name = "Old" };
        var newObject = new TestState { Name = "New" };

        var change = new PropertyChange("State", oldObject, newObject);

        Assert.Same(oldObject, change.OldValue);
        Assert.Same(newObject, change.NewValue);
    }
}
