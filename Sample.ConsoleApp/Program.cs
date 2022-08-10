namespace Sample.ConsoleApp;

// Refactor fron `internal class` to `partial class`
partial class Program
{
  static partial void HelloFrom(string name);

  static void Main(string[] args)
  {
    HelloFrom("my generated method Code");
  }
}
