using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class ExternalWindowsList
{
    public ExternalWindow[] windowsSet => handler.GetComponents<ExternalWindow>();
    GameObject handler = new("MWMWindowHandler");
    Dictionary<string, ExternalWindow> cachedDict = new();
    public event Action OnUpdate = new(() => { });
    public ExternalWindow this[int index]
    {
        get => windowsSet[index];
    }
    public ExternalWindow this[string id]
    {
        get => cachedDict[id];
    }

    public int Count => windowsSet.Length;
    public bool IsReadOnly => false;
    public bool Add(string id)
    {
        Debug.Log("试图新建窗口:"+id);
        if (! cachedDict.ContainsKey(id)) {
            Debug.Log("确认无重复");
            ExternalWindow newWindow = handler.AddComponent<ExternalWindow>();
            Debug.Log("在handler添加了窗口。");
            newWindow.windowId = id;
            Debug.Log("设置了窗口id为"+id);
            cachedDict.Add(id,newWindow);
            OnUpdate?.Invoke();
            Debug.Log("已通知所有更新事件运行，正在返回……");
            return true;
        }
        else return false;
    }
    public void Clear()
    {
        Debug.Log("清除所有窗口……");
        foreach (ExternalWindow item in windowsSet)
        {
            MonoBehaviour.Destroy(item);
        }
        cachedDict.Clear();
        OnUpdate?.Invoke();
    }
    public bool Contains(ExternalWindow item) => windowsSet.Contains(item);
    public IEnumerator GetEnumerator() => windowsSet.GetEnumerator();
    public int IndexOf(ExternalWindow item) => handler.GetComponentIndex(item);
    public void Remove(ExternalWindow item)
    {
        Debug.Log("移除窗口……");
        item.Close();
        cachedDict.Remove(item.windowId);
        MonoBehaviour.Destroy(item);
        OnUpdate?.Invoke();
    }
    public void RemoveById(string id)
    {
        Debug.Log("移除窗口……");
        cachedDict.Remove(id);
        MonoBehaviour.Destroy(this[id]);

        OnUpdate?.Invoke();
    }
    public void RenameItem(string id,string newId)
    {
        ExternalWindow window = this[id];
        window.windowId = newId;
        cachedDict.Remove(id);
        cachedDict[newId] = window;
    }
    public ExternalWindowsList() 
    {
        OnUpdate += () => Debug.Log("芜！OnUpdate被调用了！");
    }
}

