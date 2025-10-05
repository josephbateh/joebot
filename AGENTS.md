# Agent Development Guidelines

## Development Cycle

1. Read the relevant code to understand it.
2. Decide what changes need to be made to accomplish your task.
3. Make changes to the code.
4. Build the code with `dotnet build`.
5. Run the code with `dotnet run`.
6. If the code works as expected, proceed to the next step. If not, go back to step 1.
7. Write tests that verify the functionality of the code.
8. Run tests using `dotnet test`.
9. If tests fail, go back to step 1.

## Testing Direction

1. Write tests that cover execution of the command from the top level command parser.
2. Write tests that are more like integration tests than unit tests.
3. Any systems below should use adapters to allow for high-confidence testing.

### File System

1. File system calls should go through a `FileSystemUtils` class that wraps any commands to the file system.
2. The `FileSystemUtils` class should use an adapter to directly pass through to normal file system calls when not
   running under tests.
3. The `FileSystemUtils` class should use `Files.FileSystems.InMemory` when running under tests.

### Date and Time

1. Date and time calls should go through a `DateTimeUtils` class that wraps any calls to get the current date and time.
2. The `DateTimeUtils` class should use an adapter to directly pass through to normal date and time calls when not running
   under tests.
3. The `DateTimeUtils` class should use `TimeProvider` when running under tests.

## Rules

1. Do not use `rm` for `bin` or `obj` files when building. Instead, use `dotnet clean` to remove these files.
