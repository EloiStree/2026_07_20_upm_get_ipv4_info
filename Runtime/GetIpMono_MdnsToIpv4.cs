using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Eloi.GetIpInfo
{
    [Serializable]
    public class StringIpEvent : UnityEvent<string> { }

    public class GetIpMono_MdnsToIpv4 : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("e.g. raspberrypi.local, steamdeck.local, myhost.ddns.net")]
        public string m_host = "raspberrypi.local";

        [Header("Events")]
        public StringIpEvent m_onFirstIpv4Found = new StringIpEvent();
        public StringIpEvent m_onAnyIpv4Found = new StringIpEvent();
        public UnityEvent m_onResolveFailed = new UnityEvent();

        [Header("State")]
        [SerializeField] private string m_lastResolvedIpv4 = "";
        [SerializeField] private bool m_resolvedAtLeastOnce = false;
        [SerializeField] private string m_lastError = "";

        [Header("Auto")]
        public bool m_fetchOnStart = true;
        public bool m_pollEveryFewSeconds = false;
        public float m_pollIntervalSeconds = 5f;
        private float _nextPollTime;


        [ContextMenu("Set Host: Raspberry Pi")]
        public void SetHostRaspberryPi() { m_host = "raspberrypi.local"; FetchNow(); }

        [ContextMenu("Set Host: Steam Deck")]
        public void SetHostSteamDeck() { m_host = "steamdeck.local"; FetchNow(); }

        [ContextMenu("Set Host: ESP32 / Espressif")]
        public void SetHostEspressif() { m_host = "espressif.local"; FetchNow(); }

        [ContextMenu("Set Host: BeagleBone")]
        public void SetHostBeagleBone() { m_host = "beaglebone.local"; FetchNow(); }

        [ContextMenu("Set Host: Odroid")]
        public void SetHostOdroid() { m_host = "odroid.local"; FetchNow(); }

        [ContextMenu("Set Host: Arduino")]
        public void SetHostArduino() { m_host = "arduino.local"; FetchNow(); }

        [ContextMenu("Resolve Now")]
        public void ContextMenuResolveNow() => FetchNow();

        
        public void FetchNow() => ResolveAsync(m_host);

        public void Resolve(string host) => ResolveAsync(host);

        public static string ResolveToIpv4(string host)
        {
            if (string.IsNullOrWhiteSpace(host)) return null;
            try
            {
                IPAddress[] addrs = Dns.GetHostAddresses(host);
                foreach (var a in addrs)
                    if (a.AddressFamily == AddressFamily.InterNetwork)
                        return a.ToString();
            }
            catch (Exception e) { Debug.LogWarning($"[MdnsToIpv4] Resolve failed for '{host}': {e.Message}"); }
            return null;
        }

        private CancellationTokenSource _cts;

        private async void ResolveAsync(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                m_lastError = "Host is empty.";
                m_onResolveFailed?.Invoke();
                return;
            }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            string firstIpv4 = null;
            string error = null;

            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    IPAddress[] addrs = Dns.GetHostAddresses(host);
                    foreach (var a in addrs)
                    {
                        if (a.AddressFamily == AddressFamily.InterNetwork)
                        {
                            firstIpv4 = a.ToString();
                            break;
                        }
                    }
                    if (firstIpv4 == null)
                        error = $"Host '{host}' resolved but had no IPv4 (only IPv6?).";
                }
                catch (SocketException sx) { error = $"Socket: {sx.SocketErrorCode} - {sx.Message}"; }
                catch (Exception ex) { error = ex.Message; }
            }, token);

            if (token.IsCancellationRequested) return;

            if (!string.IsNullOrEmpty(firstIpv4))
            {
                m_lastError = "";
                m_lastResolvedIpv4 = firstIpv4;

                if (!m_resolvedAtLeastOnce)
                {
                    m_resolvedAtLeastOnce = true;
                    m_onFirstIpv4Found?.Invoke(firstIpv4);
                }
                m_onAnyIpv4Found?.Invoke(firstIpv4);
            }
            else
            {
                m_lastError = error ?? "Unknown resolve error.";
                Debug.LogWarning($"[MdnsToIpv4] {m_lastError}");
                m_onResolveFailed?.Invoke();
            }
        }

        private void OnDestroy() => _cts?.Cancel();


        private void Start()
        {
            if (m_fetchOnStart) FetchNow();
            _nextPollTime = Time.time + m_pollIntervalSeconds;
        }

        private void Update()
        {
            if (m_pollEveryFewSeconds && Time.time >= _nextPollTime)
            {
                _nextPollTime = Time.time + m_pollIntervalSeconds;
                FetchNow();
            }
        }
    }
}