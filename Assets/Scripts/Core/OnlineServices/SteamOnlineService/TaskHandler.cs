using System.Threading.Tasks;

namespace Core.OnlineServices.SteamOnlineService {
    public static class TaskHandler {
        
        public static void RecreateTask<T>(ref TaskCompletionSource<T> task) {
            if (task != null && !task.Task.IsCompleted) {
                task.SetCanceled();
            }

            task = new TaskCompletionSource<T>();
        }
    }
}