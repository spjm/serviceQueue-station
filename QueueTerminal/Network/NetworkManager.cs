﻿using QueueTerminal.Interfaces;
using QueueTerminal.Models;
using QueuServer.Managers;
using QueuTerminal.Models.Terminal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QueuServer
{
    class ServerManager
    {
        IServerUpdateListener listener;

        TcpClient tcpClient;
        NetworkStream stream;
        Thread thread;

        public ServerManager(IServerUpdateListener listener)
        {
            this.listener = listener;
        }

        ~ServerManager()
        {
            stream.Close();
        }

        public void Initialize()
        {
            tcpClient = new TcpClient();
            tcpClient.Connect(Constants.IP_ADDRESS, Constants.IP_PORT);
            stream = tcpClient.GetStream();

            thread = new Thread(() => ListenToServer(stream));
            thread.Start();

            SendIdentification();
        }

        private void SendIdentification()
        {
            // We will identify ourselves to the server so it can send us
            // only the information we need in the future.

            var id = new ConnectionIdentification();
            var json = SerializationManager<ConnectionIdentification>.Serialize(id);
            var task = SendDataToServerAsync(json);
        }

        private void ListenToServer(NetworkStream stream)
        {
            if (stream == null)
                return;

            while (true)
            {
                var buffer = new byte[Constants.bufferSize];
                int size = stream.Read(buffer, 0, buffer.Length);
                if (size > 0)
                    ComputeServerRequest(buffer, size);
                else
                {
                    //TODO remove socket
                    throw new Exception();
                }
            }
        }

        private void ComputeServerRequest(byte[] buffer, int size)
        {
            var bArray = new byte[size];
            Array.Copy(buffer, bArray, size);

            listener.NewListReceived(SerializationManager<ServerUpdate>.Desserialize(bArray));
        }

        public async Task SendDataToServerAsync(string data)
        {
            if (stream == null || !stream.CanWrite || data == null || data.Length == 0)
                return;

            var buffer = Encoding.ASCII.GetBytes(data);
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }
    }
}