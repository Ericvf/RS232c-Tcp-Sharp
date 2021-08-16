﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DotnetRS232cLAN
{
    public sealed class RS232cTcpClient : IRS232cTcpClient, IDisposable
    {
        private readonly ILogger<RS232cTcpClient> logger;
        private readonly Dictionary<Commands, string> _commandTexts = new Dictionary<Commands, string>()
        {
            { Commands.PowerControl, "POWR" },
            { Commands.InputModeSelection, "INPS" },
            { Commands.Brightness, "VLMP" },
            { Commands.Size, "WIDE" },
            { Commands.ColorMode, "BMOD" },
            { Commands.WhiteBalance, "WHBL" },
            { Commands.RContrast, "CRTR" },
            { Commands.GContrast, "CRTG" },
            { Commands.BContrast, "CRTB" },
            { Commands.TelnetUsername, "USER" },
            { Commands.TelnetPassword, "PASS" },
            { Commands.ThermalSensorSetting, "STDR" },
            { Commands.Model, "INF1" },
            { Commands.SerialNo, "SRNO" },
            { Commands.Auto, "ASNC" },
            { Commands.Reset, "RSET" },
            { Commands.Volume, "VOLM" },
            { Commands.Mute, "MUTE" },
            { Commands.TemperatureSensor, "DSTA" },
            { Commands.Temperature, "ERRT" },
        };
        private const int timeout = 200;

        private TcpClient? tcpClient;
        private NetworkStream? netstream;
        private StreamWriter? writer;
        private StreamReader? reader;

        public RS232cTcpClient(ILogger<RS232cTcpClient> logger)
        {
            this.logger = logger;
        }

        public async Task Start(string ipAddress, int port, string? username = null, string? password = null)
        {
            tcpClient = new TcpClient();

            await tcpClient.ConnectAsync(ipAddress, port);

            netstream = tcpClient.GetStream();
            writer = new StreamWriter(netstream);
            reader = new StreamReader(netstream);

            writer.AutoFlush = true;
            netstream.ReadTimeout = 500;

            string input, output;

            output = await ReadOutput(netstream, reader);
            logger.LogInformation(output);

            if (output == "Login:")
            {
                input = string.Empty;
                writer.WriteLine(input);
                output = await ReadOutput(netstream, reader);
                logger.LogInformation(output);
            }

            if (output == "\r\nPassword:")
            {
                input = string.Empty;
                writer.WriteLine(input);

                output = await ReadOutput(netstream, reader);
                logger.LogInformation(output); 
            }
        }

        public async Task Stop()
        {
            await SendCommandAndGetResponse("BYE");
            netstream?.Dispose();
        }

        public Task<string> Get(Commands command)
        {
            var commandString = GetCommandString(command);
            return SendCommandAndGetResponse($"{commandString}????");
        }

        public Task<string> Set(Commands command, int value)
        {
            var commandString = GetCommandString(command);
            return SendCommandAndGetResponse($"{commandString}{Pad(value)}");
        }

        private string GetCommandString(Commands command) => _commandTexts[command];

        public Task<string> Get(string command) => SendCommandAndGetResponse(command);

        private string Pad(int value) => Convert.ToString(value).PadLeft(4, '0');

        private Task<string> SendCommandAndGetResponse(string command)
        {
            logger.LogInformation("SendCommandAndGetResponse", command);

            writer!.WriteLine(command);
            return ReadOutput(netstream!, reader!);
        }

        private async Task<string> ReadOutput(NetworkStream netstream, StreamReader reader)
        {
            await Task.Delay(timeout);

            var stringBuilder = new StringBuilder();
            while (netstream.DataAvailable)
            {
                stringBuilder.Append((char)netstream.ReadByte());
            }

            return stringBuilder.ToString();
        }

        public bool IsConnected() => tcpClient?.Connected == true;

        public void Dispose()
        {
            netstream?.Dispose();
        }
    }
}