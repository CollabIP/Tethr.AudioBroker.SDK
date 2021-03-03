![logo](https://github.com/CollabIP/Tethr.AudioBroker.SDK/blob/master/src/tethr-96.png?raw=true)
# Tethr.AudioBroker.SDK
SDK for sending calls, chats, and cases to Tethr.

[NuGet Package](https://www.nuget.org/packages/Tethr.AudioBroker/)

### Breaking Changes for 2.x
The parameterless constructor for `TethrSession` has been removed.
This was used to configure the session automatically by reading the Tethr connection string
from the app config but the app config is no longer read directly by the SDK. The host
application is now responsible configuring the `TethrSession`.
A new constructor is available that takes a `TethrSessionOptions` object and it has a static method
available that creates a new instance using a connection string. Please see the `UploadRecordingSample`
application for an example of how to configure the session from a connection string in the app config.

