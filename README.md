# EasyState.Blazor

A lightweight, reactive state management library for Blazor applications, designed to be simple, intuitive, and powerful. Built on top of `System.Reactive`, it provides a clean way to manage shared application state and communicate between components.

## Features

- **Centralized State Management**: Keep your application's state in one place, making it predictable and easy to debug.
- **Reactive API**: Use observables to listen for state changes and automatically update your UI.
- **Component Communication**: A built-in Event Aggregator allows decoupled components to communicate seamlessly.
- **Scoped or Singleton**: Easily configure state to be per-user (scoped) or global (singleton).
- **Boilerplate Reduction**: `StateComponentBase` provides a convenient starting point for your components, handling dependency injection and subscription management automatically.

## Getting Started

### 1. Installation

This project is not yet on NuGet. To use it, you will need to clone this repository and reference the `EasyState.Blazor` project.

### 2. Register Services

In your Blazor application's `Program.cs`, register the EasyState services. You can choose between scoped or singleton lifetimes.

For most Blazor Server applications, **scoped** is the correct choice. This creates a new state container for each user circuit.

```csharp
// Program.cs
using EasyState.Blazor;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add EasyState for per-user state management
builder.Services.AddStateManagement();

// ... other services

var app = builder.Build();
```

If you need a single state shared across **all users** of your application, use `AddGlobalStateManagement`.

```csharp
// Use for application-wide global state
builder.Services.AddGlobalStateManagement();
```

## Usage

### 1. Define Your State

Create simple POCO classes to represent the pieces of your application state.

```csharp
// States/CounterState.cs
public class CounterState
{
    public int CurrentCount { get; set; } = 0;
}
```

### 2. Create a Component

Inherit from `StateComponentBase` to get easy access to state and event management helpers.

In this example, we'll create a simple counter component.

```razor
@page "/counter"
@inherits StateComponentBase

<h3>Counter</h3>

<p>Current count: @CurrentCount</p>

<button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

@code {
    private int CurrentCount { get; set; }

    protected override void OnInitialized()
    {
        // ObserveState subscribes to changes in CounterState.
        // It automatically updates the component when the state changes.
        ObserveState<CounterState>(state =>
        {
            CurrentCount = state.CurrentCount;
        });
    }

    private void IncrementCount()
    {
        // UpdateState provides a safe way to modify the state.
        UpdateState<CounterState>(state =>
        {
            state.CurrentCount++;
        });
    }
}
```

**How it works:**
- `StateComponentBase` injects the `IAppState` service.
- `OnInitialized()` sets up a subscription using `ObserveState<CounterState>()`. Whenever the `CounterState` is modified anywhere in the app, the provided lambda is executed, and the component's UI is automatically re-rendered.
- `IncrementCount()` calls `UpdateState<CounterState>()`, which gets the current state, applies the update action, and notifies all subscribers.

### 3. Component-to-Component Communication

Use the `IEventAggregator` to send messages between components that don't have a direct parent-child relationship.

First, define the event message:
```csharp
// Events/NotificationEvent.cs
public class NotificationEvent
{
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
}
```

**Sender Component**
This component publishes a `NotificationEvent`.

```razor
@inherits StateComponentBase

<button @onclick="SendNotification">Send Notification</button>

@code {
    private void SendNotification()
    {
        PublishEvent(new NotificationEvent
        {
            Message = "Hello from the sender!",
            Timestamp = DateTime.UtcNow
        });
    }
}
```

**Receiver Component**
This component subscribes to `NotificationEvent` and displays the message.

```razor
@inherits StateComponentBase
@implements IDisposable

<h3>Notifications</h3>

@if (!string.IsNullOrEmpty(LastMessage))
{
    <p>@LastMessage</p>
}
else
{
    <p>Waiting for a notification...</p>
}


@code {
    private string LastMessage { get; set; } = "";

    protected override void OnInitialized()
    {
        // Subscribe to the event. The handler will be called
        // each time a NotificationEvent is published.
        SubscribeEvent<NotificationEvent>(HandleNotification);
    }

    private void HandleNotification(NotificationEvent e)
    {
        LastMessage = $"'{e.Message}' at {e.Timestamp:T}";
    }
}
```

`StateComponentBase` automatically handles unsubscriptions when the component is disposed.

## API Overview

### `IAppState`
The core service for managing state objects.
- `T GetState<T>()`: Retrieves the current instance of a state object, creating it if it doesn't exist.
- `void SetState<T>(T state)`: Overwrites a state object completely.
- `void UpdateState<T>(Action<T> updateAction)`: Performs a thread-safe update on a state object.
- `IObservable<T> ObserveState<T>()`: Returns an observable that emits the latest state and then any subsequent changes.

### `IEventAggregator`
A service for pub/sub messaging.
- `void Publish<TEvent>(TEvent eventData)`: Publishes an event to all subscribers.
- `IObservable<TEvent> Subscribe<TEvent>()`: Returns an observable for listening to events of a specific type.

### `StateComponentBase`
An abstract base component that simplifies interaction with the state and event services. It manages subscriptions and ensures the UI is updated in response to changes. All subscriptions are automatically disposed of when the component is destroyed.
