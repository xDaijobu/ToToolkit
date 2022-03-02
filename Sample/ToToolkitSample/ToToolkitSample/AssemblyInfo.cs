using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

//Enabling C# 9 in Xamarin & .NET Standard Projects
//https://dev.to/dotnet/enabling-c-9-in-xamarin-net-standard-projects-338
namespace System.Runtime.CompilerServices
{
    public class IsExternalInit { }
}