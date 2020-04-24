using Code.Server;
using LiteNetLib;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Client
{
    public class UiController : MonoBehaviour
    {
        [SerializeField] private GameObject _uiObject;
        [SerializeField] private ClientLogic _clientLogic;
        [SerializeField] private ServerLogic _serverLogic;
        [SerializeField] private InputField _ipField;
        [SerializeField] private Text _disconnectInfoField;

        private void Awake()
        {
            _ipField.text = NetUtils.GetLocalIp(LocalAddrType.IPv4);
        }

        public void OnHostClick()
        {
            _serverLogic.StartServer();
            _uiObject.SetActive(false);
            _clientLogic.Connect("localhost", OnDisconnected);
        }

        private void OnDisconnected(DisconnectInfo info)
        {
            _uiObject.SetActive(true);
            _disconnectInfoField.text = info.Reason.ToString();
        }

        public void OnConnectClick()
        {
            _uiObject.SetActive(false);
            _clientLogic.Connect(_ipField.text, OnDisconnected);
        }
    }
}
