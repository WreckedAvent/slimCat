# Contributing

## Identifying Issues

If it's not on the issue page, it's not something I'm working on -- or maybe even aware of. Other contributors won't be aware of it either! So even if you're not a developer, adding some issues for things you'd like to see resolved/added/fixed is the best way to get them in a later release.

## Pull Requests

Pull requests are accepted, but will be code reviewed for quality and consistency of style with the rest of the code base. Look around for how things are done or ask questions if you want to minimize the time this process takes. 

Because this might take a few days, it is recommended that you issue the PR on a branch **that is not your master/develop** so you can do work on other things without being blocked on the progress of the PR --- *especially* if it is rejected.

## Finding Things

If you're completely new to the code base, it might be difficult to find things. Here's some things to be aware of:

 * The code base is written with the unity container using dependency injection. This means *client code rarely `new`s up an object*, and instead has it 'injected' into its constructor.
 ** This also means all services/injectables must work against an interface.
 ** And then registered in `bootstrapper.cs`.
 * The code base follows MVVM architecture (mostly). This means that:
 ** Models are mostly plain old C# objects (`POCO`s) which do not have much logic them, only the "shapes" of data
 ** Views are XAML/.cs and handle user interaction. All of the styling and most of the interaction logic is done with XAML, with some more "code behind" code to handle some other cases (such as keybinds).
 ** Viewmodels consume models and are consumed by views. This is where most of the business logic lays, such as what model changes occur when a user clicks a button. This is also where you'll find the source of bindings. The Viewmodel is responsible for translating the model into something which is bindable and suitable for display on the UI.
 ** Services are where business logic that is not strictly related to bindings is concerned. This is where you'll find the glue that handles server commands or translates user commands.
 ** Specifically to WPF, common view-specific logic is often put into a *converter*.
 ** The `Utilities` is mostly just a grabbing of otherwise common code. All extension methods are here, as well as constants.
 * slimCat does *not* use a behavior library
 * silmCat does *not* use Telrik or any other vendor-provided controls. All are hand-written XAML (I don't even like blend). 

### Making a dummy service

As an example of how to make a service, let's create a new `dummy.cs` file in `services`. We'll add some code to it:

```c#
public class DummyService 
{
  private IEventAggregator events;
  public DummyService(IChatState chatState) 
  {
    events = chatState.EventAggregator;
  }
}
```

It doesn't do very much right now. Let's add an event handler that does something more interesting.

```c#
events.GetEvent<UserCommandEvent>()
      .Subscribe(command => Console.WriteLine(command.Get(Constants.Arguments.Type)));
```

Cool. Now whenever the user tries to send a user command, we'll write to console what kind of command it was. So if they enter `/busy I'm busy now` we'll get `busy` in our console. Well, almost. We just have to do one more step to make this work. Find `bootstrapper.cs`, then look for a bunch of lines starting with `Instantiate`. Add the folowing to the bottom:

```c#
Instantiate<DummyService>();
```

Compile, run, and now you'll get stuff going to console! Later on, if you make this service adhere to an interface, you'll have to use `RegisterSingleton` to get the DI to understand the two belong to one another.

### Other Points of Interest

 * All command implementations are in the `Commands` folder.
 * Commands that the user enters go through `CommandParser` -> `CommandDefinitions` -> `UserCommandService` -> `A command implementation` -> and then potentially `FchatService`
 * Commands that the server sends go through `FchatService` -> `ServerCommandService` -> `a command implementation`
 * The `CommandService` files are needed to pair a given command name with a command implementation. `CommandDefinitions` describe the 'shape' of user commands and is entirely for data extraction/validation.
 * A new update (something which shows up in the update panel) can be used with `events.Update(command)`. These too have handlers.
 * `NotificationService` handles toasts, triggering dings on new toasts, plays sounds, etc.
 * `IconService` handles the tray icon
 * `ChannelService` handles adding/removing channels
 * `IChatState` should contain most of the things you'd want to interact with

## Compiling, Making a Release

You should not need to do anything special to get slimCat to build. Should you need to make a release to a user, make a `release` build and run `build.ps1`. This will create a `build` folder organized as you'd need to be consumed by a user. Because I don't pay any monies to have the code signed, and you probably won't either, be aware that users will see a warning from windows saying the code was downloaded from the internet and probably not safe. They will have to know to dismiss that.