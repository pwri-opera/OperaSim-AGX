using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.RenderStreaming;
using Unity.WebRTC;
using System.Linq;

namespace PWRISimulator
{
    public class CameraSignalingHandler : SignalingHandlerBase, IOfferHandler, IDisconnectHandler, IDeletedConnectionHandler
    {
        public List<VideoStreamSender> CameraStreamList = new List<VideoStreamSender>();
        //public GameObject CameraGroup;

        private List<string> connectionIds = new List<string>();

        //rtiStreamIdで指定したindexに対応するVideoStreamSenderを返す
        private GameObject GetSelectedVideoStreamSender(string rtiStreamId)
        {
            var items = CameraStreamList;
            UnityEngine.Debug.Log(items.Count);

            int streamIndex = int.Parse(rtiStreamId);

            //指定したIndexを持つVideoStreamSenderを返す
            if (streamIndex < items.Count)
            {
                UnityEngine.Debug.Log("VideoStreamSender Obj (GetSelectedVideoStreamSender) : " + items[streamIndex].gameObject.name);
                return items[streamIndex].gameObject;
            }

            //対応するindexが存在しない場合にはリストの最初のVideoStreamSenderを返す
            UnityEngine.Debug.Log("Error: Selected Stream Index is Out Of Number Of VideoStremaSenders.");
            return items[0].gameObject;

        }


        //Offer処理
        public void OnOffer(SignalingEventData data)
        {
            //VideoStreamSenderオブジェクトを検索しリストに登録
            var items = FindObjectsOfType<VideoStreamSender>();
            foreach (var item in items)
            {
                UnityEngine.Debug.Log("VideoStreamSender Obj (OnOffer) : " + item.gameObject.name);
                if (!CameraStreamList.Contains(item)) CameraStreamList.Add(item);
            }

            //Connection IDの管理
            if (connectionIds.Contains(data.connectionId))
            {
                UnityEngine.Debug.Log("Already answered this connectionId : " + data.connectionId);
                return;
            }
            connectionIds.Add(data.connectionId);

            //sdpデータを要素毎に分解し、sdpデータに含まれているridを取得
            var sdpList = data.sdp.Split("\r\n");
            var results = sdpList.Where(line => line.Contains("a=rid"));
            var res = "";
            foreach (var line in results)
            {
                res = line;
                break;
            }

            GameObject gameObject = null;

            //ridが取得可能であればridに対応するVideoStreamSenderを取得
            if (res != "")
            {
                UnityEngine.Debug.Log("test2 : " + res.Split("=")[1].Split(":")[1]);
                gameObject = GetSelectedVideoStreamSender(res.Split("=")[1].Split(":")[1]);
                if (gameObject == null)
                {
                    UnityEngine.Debug.Log($"Sorry, no more streams available to assign to connectionId : {data.connectionId}");
                    return;
                }

            }
            else
            { //ridが取得できなければListの先頭のVideoStreamSenderを取得
                gameObject = GetSelectedVideoStreamSender("0"); ;
                if (gameObject == null)
                {
                    UnityEngine.Debug.Log($"Sorry, no more streams available to assign to connectionId : {data.connectionId}");
                    return;
                }
            }

            UnityEngine.Debug.Log($"Found an available stream, adding to connectionId : {data.connectionId}");

            gameObject.GetComponent<VideoStreamSenderProp>().connectionId = data.connectionId;

            //gameObject.name = data.connectionId;
            //gameObject.SetActive(true);

            AddSender(data.connectionId, gameObject.GetComponent<VideoStreamSender>());
            SendAnswer(data.connectionId);
        }




        //connectionIdで指定したConnectionIDを持つアクティブなVideoStreamSenderを返す
        private GameObject FindActiveVideoStreamSender(string connectionId)
        {
            //var items = CameraGroup.GetComponentsInChildren<VideoStreamSender>();
            var items = FindObjectsOfType<VideoStreamSender>();

            foreach (var item in items)
            {
                UnityEngine.Debug.Log("VideoStreamSender Obj (FindActiveVideoStreamSender) : " + item.gameObject.name);
                UnityEngine.Debug.Log("VideoStreamSender Obj (FindActiveVideoStreamSender) : " + item.gameObject.GetComponent<VideoStreamSenderProp>().connectionId);
                //if (item.gameObject.name == connectionId) return item.gameObject;
                if (item.gameObject.GetComponent<VideoStreamSenderProp>().connectionId == connectionId) return item.gameObject;
            }

            return null;
        }



        //接続切断処理
        private void Disconnect(string connectionId)
        {
            if (!connectionIds.Contains(connectionId))
            {
                return;
            }

            // Find and disable the game object
            //FindActiveVideoStreamSender(connectionId)?.SetActive(false);

            connectionIds.Remove(connectionId);
        }

        //接続切断
        public void OnDeletedConnection(SignalingEventData eventData)
        {
            Disconnect(eventData.connectionId);
        }

        //クライアント側から接続切断
        public void OnDisconnect(SignalingEventData eventData)
        {
            Disconnect(eventData.connectionId);
        }
    }
}
