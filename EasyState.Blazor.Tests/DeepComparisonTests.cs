namespace EasyState.Blazor.Tests;

public class DeepComparisonTests : IDisposable
{
    private readonly AppState _appState;

    public DeepComparisonTests()
    {
        _appState = new AppState();
    }

    public void Dispose()
    {
        _appState.Dispose();
    }

    // Test state classes with nested objects
    public class Address
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
    }

    public class Contact
    {
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    public class UserProfile
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public Address HomeAddress { get; set; } = new();
        public Address WorkAddress { get; set; } = new();
        public Contact ContactInfo { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class OrderState
    {
        public int OrderId { get; set; }
        public List<OrderItem> Items { get; set; } = new();
        public Customer Customer { get; set; } = new();
    }

    public class OrderItem
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class Customer
    {
        public string Name { get; set; } = string.Empty;
        public Address ShippingAddress { get; set; } = new();
    }

    [Fact]
    public async Task DetectChanges_NestedObject_SinglePropertyChange()
    {
        await _appState.SetState(new UserProfile
        {
            Name = "John",
            HomeAddress = new Address { Street = "123 Main St", City = "Boston", ZipCode = "02101" }
        });

        var result = await _appState.UpdateState<UserProfile>(s =>
        {
            s.HomeAddress.City = "New York";
        });

        Assert.NotNull(result);
        Assert.Single(result.ChangedProperties);
        Assert.Equal(nameof(UserProfile.HomeAddress), result.ChangedProperties[0].PropertyName);
    }

    [Fact]
    public async Task DetectChanges_NestedObject_MultiplePropertiesChanged()
    {
        await _appState.SetState(new UserProfile
        {
            Name = "John",
            HomeAddress = new Address { Street = "123 Main St", City = "Boston", ZipCode = "02101" }
        });

        var result = await _appState.UpdateState<UserProfile>(s =>
        {
            s.HomeAddress.City = "New York";
            s.HomeAddress.ZipCode = "10001";
        });

        Assert.NotNull(result);
        Assert.Single(result.ChangedProperties);
        Assert.Equal(nameof(UserProfile.HomeAddress), result.ChangedProperties[0].PropertyName);
        
        // Verify the old value
        var oldAddress = result.ChangedProperties[0].OldValue as Address;
        Assert.NotNull(oldAddress);
        Assert.Equal("Boston", oldAddress.City);
        Assert.Equal("02101", oldAddress.ZipCode);

        // Verify the new value
        var newAddress = result.ChangedProperties[0].NewValue as Address;
        Assert.NotNull(newAddress);
        Assert.Equal("New York", newAddress.City);
        Assert.Equal("10001", newAddress.ZipCode);
    }

    [Fact]
    public async Task DetectChanges_MultipleNestedObjects_Changed()
    {
        await _appState.SetState(new UserProfile
        {
            Name = "John",
            HomeAddress = new Address { Street = "123 Main St", City = "Boston" },
            WorkAddress = new Address { Street = "456 Work Ave", City = "Cambridge" }
        });

        var result = await _appState.UpdateState<UserProfile>(s =>
        {
            s.HomeAddress.City = "New York";
            s.WorkAddress.City = "Manhattan";
        });

        Assert.NotNull(result);
        Assert.Equal(2, result.ChangedProperties.Count);
        Assert.Contains(result.ChangedProperties, p => p.PropertyName == nameof(UserProfile.HomeAddress));
        Assert.Contains(result.ChangedProperties, p => p.PropertyName == nameof(UserProfile.WorkAddress));
    }

    [Fact]
    public async Task DetectChanges_NestedObject_NoChange()
    {
        await _appState.SetState(new UserProfile
        {
            Name = "John",
            HomeAddress = new Address { Street = "123 Main St", City = "Boston" }
        });

        var result = await _appState.UpdateState<UserProfile>(s =>
        {
            s.HomeAddress.City = "Boston"; // Same value
        });

        Assert.Null(result); // No changes
    }

    [Fact]
    public async Task DetectChanges_List_ItemAdded()
    {
        await _appState.SetState(new UserProfile
        {
            Name = "John",
            Tags = new List<string> { "developer", "blogger" }
        });

        var result = await _appState.UpdateState<UserProfile>(s =>
        {
            s.Tags.Add("speaker");
        });

        Assert.NotNull(result);
        Assert.Single(result.ChangedProperties);
        Assert.Equal(nameof(UserProfile.Tags), result.ChangedProperties[0].PropertyName);

        var oldTags = result.ChangedProperties[0].OldValue as List<string>;
        var newTags = result.ChangedProperties[0].NewValue as List<string>;
        
        Assert.Equal(2, oldTags?.Count);
        Assert.Equal(3, newTags?.Count);
        Assert.Contains("speaker", newTags);
    }

    [Fact]
    public async Task DetectChanges_List_ItemRemoved()
    {
        await _appState.SetState(new UserProfile
        {
            Name = "John",
            Tags = new List<string> { "developer", "blogger", "speaker" }
        });

        var result = await _appState.UpdateState<UserProfile>(s =>
        {
            s.Tags.Remove("blogger");
        });

        Assert.NotNull(result);
        Assert.Single(result.ChangedProperties);
        Assert.Equal(nameof(UserProfile.Tags), result.ChangedProperties[0].PropertyName);
    }

    [Fact]
    public async Task DetectChanges_List_ItemModified()
    {
        await _appState.SetState(new UserProfile
        {
            Name = "John",
            Tags = new List<string> { "developer", "blogger" }
        });

        var result = await _appState.UpdateState<UserProfile>(s =>
        {
            s.Tags[0] = "senior developer";
        });

        Assert.NotNull(result);
        Assert.Single(result.ChangedProperties);
        Assert.Equal(nameof(UserProfile.Tags), result.ChangedProperties[0].PropertyName);
    }

    [Fact]
    public async Task DetectChanges_Dictionary_ItemAdded()
    {
        await _appState.SetState(new UserProfile
        {
            Name = "John",
            Metadata = new Dictionary<string, string>
            {
                { "department", "Engineering" },
                { "level", "Senior" }
            }
        });

        var result = await _appState.UpdateState<UserProfile>(s =>
        {
            s.Metadata["location"] = "Remote";
        });

        Assert.NotNull(result);
        Assert.Single(result.ChangedProperties);
        Assert.Equal(nameof(UserProfile.Metadata), result.ChangedProperties[0].PropertyName);
    }

    [Fact]
    public async Task DetectChanges_Dictionary_ValueModified()
    {
        await _appState.SetState(new UserProfile
        {
            Name = "John",
            Metadata = new Dictionary<string, string>
            {
                { "department", "Engineering" },
                { "level", "Senior" }
            }
        });

        var result = await _appState.UpdateState<UserProfile>(s =>
        {
            s.Metadata["level"] = "Staff";
        });

        Assert.NotNull(result);
        Assert.Single(result.ChangedProperties);
        Assert.Equal(nameof(UserProfile.Metadata), result.ChangedProperties[0].PropertyName);
    }

    [Fact]
    public async Task DetectChanges_ComplexNestedStructure()
    {
        await _appState.SetState(new OrderState
        {
            OrderId = 1,
            Items = new List<OrderItem>
            {
                new OrderItem { ProductName = "Laptop", Quantity = 1, Price = 999.99m }
            },
            Customer = new Customer
            {
                Name = "John Doe",
                ShippingAddress = new Address { Street = "123 Main St", City = "Boston" }
            }
        });

        var result = await _appState.UpdateState<OrderState>(s =>
        {
            s.Customer.ShippingAddress.City = "Cambridge";
        });

        Assert.NotNull(result);
        Assert.Single(result.ChangedProperties);
        Assert.Equal(nameof(OrderState.Customer), result.ChangedProperties[0].PropertyName);
    }

    [Fact]
    public async Task DetectChanges_ComplexNestedStructure_MultipleChanges()
    {
        await _appState.SetState(new OrderState
        {
            OrderId = 1,
            Items = new List<OrderItem>
            {
                new OrderItem { ProductName = "Laptop", Quantity = 1, Price = 999.99m }
            },
            Customer = new Customer
            {
                Name = "John Doe",
                ShippingAddress = new Address { Street = "123 Main St", City = "Boston" }
            }
        });

        var result = await _appState.UpdateState<OrderState>(s =>
        {
            s.Items.Add(new OrderItem { ProductName = "Mouse", Quantity = 1, Price = 29.99m });
            s.Customer.ShippingAddress.City = "Cambridge";
        });

        Assert.NotNull(result);
        Assert.Equal(2, result.ChangedProperties.Count);
        Assert.Contains(result.ChangedProperties, p => p.PropertyName == nameof(OrderState.Items));
        Assert.Contains(result.ChangedProperties, p => p.PropertyName == nameof(OrderState.Customer));
    }

    [Fact]
    public async Task DetectChanges_NullToObject()
    {
        await _appState.SetState(new UserProfile
        {
            Name = "John",
            ContactInfo = null!
        });

        var result = await _appState.UpdateState<UserProfile>(s =>
        {
            s.ContactInfo = new Contact { Email = "john@example.com", Phone = "555-1234" };
        });

        Assert.NotNull(result);
        Assert.Single(result.ChangedProperties);
        Assert.Equal(nameof(UserProfile.ContactInfo), result.ChangedProperties[0].PropertyName);
        Assert.Null(result.ChangedProperties[0].OldValue);
        Assert.NotNull(result.ChangedProperties[0].NewValue);
    }

    [Fact]
    public async Task DetectChanges_ObjectToNull()
    {
        await _appState.SetState(new UserProfile
        {
            Name = "John",
            ContactInfo = new Contact { Email = "john@example.com", Phone = "555-1234" }
        });

        var result = await _appState.UpdateState<UserProfile>(s =>
        {
            s.ContactInfo = null!;
        });

        Assert.NotNull(result);
        Assert.Single(result.ChangedProperties);
        Assert.Equal(nameof(UserProfile.ContactInfo), result.ChangedProperties[0].PropertyName);
        Assert.NotNull(result.ChangedProperties[0].OldValue);
        Assert.Null(result.ChangedProperties[0].NewValue);
    }

    [Fact]
    public async Task DetectChanges_EmptyListToPopulatedList()
    {
        await _appState.SetState(new UserProfile
        {
            Name = "John",
            Tags = new List<string>()
        });

        var result = await _appState.UpdateState<UserProfile>(s =>
        {
            s.Tags.Add("developer");
            s.Tags.Add("blogger");
        });

        Assert.NotNull(result);
        Assert.Single(result.ChangedProperties);
        Assert.Equal(nameof(UserProfile.Tags), result.ChangedProperties[0].PropertyName);
    }

    [Fact]
    public async Task DetectChanges_ReplaceEntireNestedObject()
    {
        await _appState.SetState(new UserProfile
        {
            Name = "John",
            HomeAddress = new Address { Street = "123 Main St", City = "Boston", ZipCode = "02101" }
        });

        var result = await _appState.UpdateState<UserProfile>(s =>
        {
            s.HomeAddress = new Address { Street = "456 Elm St", City = "New York", ZipCode = "10001" };
        });

        Assert.NotNull(result);
        Assert.Single(result.ChangedProperties);
        Assert.Equal(nameof(UserProfile.HomeAddress), result.ChangedProperties[0].PropertyName);

        var oldAddress = result.ChangedProperties[0].OldValue as Address;
        var newAddress = result.ChangedProperties[0].NewValue as Address;

        Assert.Equal("Boston", oldAddress?.City);
        Assert.Equal("New York", newAddress?.City);
    }

    [Fact]
    public async Task ObserveStateChanges_ReceivesNestedObjectChanges()
    {
        await _appState.SetState(new UserProfile
        {
            Name = "John",
            HomeAddress = new Address { Street = "123 Main St", City = "Boston" }
        });

        StateChange<UserProfile>? receivedChange = null;
        var observable = _appState.ObserveStateChanges<UserProfile>();
        using var subscription = observable.Subscribe(change => receivedChange = change);

        await _appState.UpdateState<UserProfile>(s =>
        {
            s.HomeAddress.City = "New York";
        });

        await Task.Delay(50);

        Assert.NotNull(receivedChange);
        Assert.Single(receivedChange.ChangedProperties);
        Assert.Equal(nameof(UserProfile.HomeAddress), receivedChange.ChangedProperties[0].PropertyName);
    }

    [Fact]
    public async Task DetectChanges_DeeplyNestedObject_ThreeLevels()
    {
        await _appState.SetState(new OrderState
        {
            OrderId = 1,
            Customer = new Customer
            {
                Name = "John",
                ShippingAddress = new Address
                {
                    Street = "123 Main St",
                    City = "Boston",
                    ZipCode = "02101"
                }
            }
        });

        var result = await _appState.UpdateState<OrderState>(s =>
        {
            s.Customer.ShippingAddress.ZipCode = "02102";
        });

        Assert.NotNull(result);
        Assert.Single(result.ChangedProperties);
        Assert.Equal(nameof(OrderState.Customer), result.ChangedProperties[0].PropertyName);
    }

    [Fact]
    public async Task DetectChanges_AsyncUpdate_WithNestedChanges()
    {
        await _appState.SetState(new UserProfile
        {
            Name = "John",
            HomeAddress = new Address { Street = "123 Main St", City = "Boston" }
        });

        var result = await _appState.UpdateState<UserProfile>(async s =>
        {
            await Task.Delay(10);
            s.HomeAddress.City = "New York";
        });

        Assert.NotNull(result);
        Assert.Single(result.ChangedProperties);
        Assert.Equal(nameof(UserProfile.HomeAddress), result.ChangedProperties[0].PropertyName);
    }

    [Fact]
    public async Task DetectChanges_MixedPrimitiveAndComplexChanges()
    {
        await _appState.SetState(new UserProfile
        {
            Name = "John",
            Age = 30,
            HomeAddress = new Address { Street = "123 Main St", City = "Boston" }
        });

        var result = await _appState.UpdateState<UserProfile>(s =>
        {
            s.Name = "John Doe";
            s.Age = 31;
            s.HomeAddress.City = "New York";
        });

        Assert.NotNull(result);
        Assert.Equal(3, result.ChangedProperties.Count);
        Assert.Contains(result.ChangedProperties, p => p.PropertyName == nameof(UserProfile.Name));
        Assert.Contains(result.ChangedProperties, p => p.PropertyName == nameof(UserProfile.Age));
        Assert.Contains(result.ChangedProperties, p => p.PropertyName == nameof(UserProfile.HomeAddress));
    }
}
