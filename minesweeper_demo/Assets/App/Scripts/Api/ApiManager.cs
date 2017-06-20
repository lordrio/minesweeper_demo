using UnityEngine;
using UniRx;

public static class ApiManager
{
    public static void SaveMine(string name, string data, int solvetime, System.Action callback)
    {
        ScheduledNotifier<float> progress = new ScheduledNotifier<float>();
        var watcher = progress.Subscribe(prog => Debug.Log(prog));//進行度の表示

        var form = new WWWForm();
        form.AddField("created_by", name);
        form.AddField("data", data);
        form.AddField("solve_time", solvetime);
        ObservableWWW.Post("http://moonlightcube.com/insertmine.php", form, progress)
            .Subscribe(result =>
            {
                    Debug.Log(result);

                    if(callback != null)
                    {
                        callback();
                    }

                    watcher.Dispose();
                    watcher = null;
            });
    }

    public static void LoadMine(System.Action<Data.MineData.MineDataList> callback)
    {
        ScheduledNotifier<float> progress = new ScheduledNotifier<float>();
        var watcher = progress.Subscribe(prog => Debug.Log(prog));//進行度の表示

        ObservableWWW.Get("http://moonlightcube.com/getmine.php", null, progress)
            .Subscribe(result =>
            {
                    Debug.Log(result);

                    var list = Data.MineData.CreateFromJSON(result);
                    foreach(var i in list.data)
                    {
                        Debug.Log(i.ToString());
                    }

                    if(callback != null)
                    {
                        callback(list);
                    }

                    watcher.Dispose();
                    watcher = null;
            });
    }
}