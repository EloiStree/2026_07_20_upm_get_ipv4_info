using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Eloi.GetIpInfo
{
    public class GetIpMono_PublicAddress : MonoBehaviour
    {
        public UnityEvent<string> m_onPublicIpv4Updated;
        public UnityEvent m_onPublicIpv4NotFoundUpdated;

        public bool m_fetchAtStart = true;

        [Header("Debug")]
        public string m_lastPublicIpv4 = "";

        void Start()
        {
            if (m_fetchAtStart)
            {
                FetchPublicIpv4();
            }
        }

        public void FetchPublicIpv4()
        {
            StartCoroutine(FetchPublicIpv4Routine());
        }

        private IEnumerator FetchPublicIpv4Routine()
        {
            string url = "https://api.ipify.org";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success && webRequest.responseCode == 200)
                {
                    string ip = webRequest.downloadHandler.text.Trim();

                    m_lastPublicIpv4 = ip;
                    m_onPublicIpv4Updated?.Invoke(ip);
                }
                else
                {
                    m_lastPublicIpv4 = "";
                    m_onPublicIpv4NotFoundUpdated?.Invoke();
                }
            } 
        }
    }
}