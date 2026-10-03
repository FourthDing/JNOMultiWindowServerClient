using Assets.Scripts.Ui;
using ModApi;
using ModApi.Ui;
using System;
using System.Collections.Generic;
using UI.Xml;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts
{
    internal static class IXmlElementExtension
    {

        public static IXmlElement[] GetChildren(this IXmlElement item)
        {
            List<IXmlElement> result = new();
            for (int i = 0; i < item.GameObject.transform.childCount; i++)
            {
                result.Add(item.GameObject.transform.GetChild(i).GetComponent<XmlElement>());
            }
            return result.ToArray();
        }
    }
}