using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;

namespace Eloi.GetIpInfo
{
    public class GetIpMono_LocalAddresses : MonoBehaviour
    {
        [Header("Settings")]
        public bool m_fetchAtReady = true;
        public bool m_removeLocalhostIpv4 = true;
        public string m_joinSplitter = ", ";
        public bool m_filterOutIpv6 = true;

        [Header("Output")]
        public List<string> m_listIpv4Addresses = new List<string>();
        public string m_ipv4AsStringJoined = "";

        [Header("Events (Signals)")]
        public UnityEvent<string> m_onIpv4ListJoinedUpdated;
        public UnityEvent<string[]> m_onIpv4ListUpdated;

        void Start()
        {
            if (m_fetchAtReady)
            {
                FetchLocalIpv4();
            }
        }

        public bool IsIpv4AddressValid(string ipv4)
        {
            if (string.IsNullOrEmpty(ipv4)) return false;

            string[] parts = ipv4.Split('.');
            if (parts.Length != 4)
            {
                return false;
            }

            foreach (string part in parts)
            {
                if (int.TryParse(part, out int num))
                {
                    if (num < 0 || num > 255)
                    {
                        return false;
                    }
                }
                else
                {
                    return false; 
                }
            }
            return true;
        }

        public void FetchLocalIpv4()
        {
            m_listIpv4Addresses.Clear();

            try
            {
                string hostName = Dns.GetHostName();
                IPAddress[] addresses = Dns.GetHostAddresses(hostName);
                foreach (IPAddress address in addresses)
                {
                    string ip = address.ToString();
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (m_removeLocalhostIpv4 && (ip == "127.0.0.1" || ip == "localhost"))
                        {
                            continue;
                        }

                        if (m_filterOutIpv6 && !IsIpv4AddressValid(ip))
                        {
                            continue;
                        }

                        m_listIpv4Addresses.Add(ip);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error fetching local IP addresses: {e.Message}");
            }

            m_onIpv4ListUpdated?.Invoke(m_listIpv4Addresses.ToArray());
            m_ipv4AsStringJoined = string.Join(m_joinSplitter, m_listIpv4Addresses);
            m_onIpv4ListJoinedUpdated?.Invoke(m_ipv4AsStringJoined);
        }
    }
}