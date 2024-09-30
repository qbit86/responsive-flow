# ResponsiveFlow

ResponsiveFlow is a minimalist WPF application that measures the time of HTTP requests.

## Usage

The application accepts the _appsettings.json_ configuration file as input.
Depending on the build configuration, _appsettings.Development.json_ or _appsettings.Production.json_ is also an option, which is convenient to override the default configuration when running from the IDE.
See _src/ResponsiveFlow.Application/appsettings-example.json_ for a reference.
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "Project": {
    "Urls": [
      "https://jsonplaceholder.typicode.com/users/",
      "https://picsum.photos/seed/1729/350/200",
      "https://placeholder.pics/svg/350x200"
    ],
    "OutputDir": "C:/Temp/"
  }
}
```

The continuous progress bar at the bottom of the window shows overall responsiveness rather than actual progress.

![](./assets/screenshot.png)

## Implementation details

The solution consists of three projects — Application, Presentation, and Models.

The Application entry point `App.Main()` serves as the composition root for setting up dependencies in the DI container.

The Presentation project provides the viewmodels and defines the UI logic.

All the asynchronous machinery for making requests and collecting the measurements is placed in the Models project.
The primary means of synchronization are channels and thread-safe data structures like `ConcurrentBag<T>` (no explicit locks so far!)
