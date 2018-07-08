using System.Threading.Tasks;

// csharp: hina/threading/taskextensions.cs [snipped]
namespace Hina.Threading
{
    public static class TaskExtensions
    {
        public static void Forget(this Task task) { }
    }
}
