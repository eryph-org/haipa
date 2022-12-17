﻿using System;
using System.Text.Json;

namespace Eryph.Messages.Operations.Events
{
    [SubscribesMessage(MessageSubscriber.Controllers)]
    public class OperationTaskStatusEvent : IOperationTaskMessage
    {
        // ReSharper disable once UnusedMember.Global
        // required for serialization
        public OperationTaskStatusEvent()
        {
        }

        protected OperationTaskStatusEvent(Guid operationId, Guid initiatingTaskId, Guid taskId, bool failed, string messageType,
            string messageData)
        {
            OperationId = operationId;
            InitiatingTaskId = initiatingTaskId;
            TaskId = taskId;
            OperationFailed = failed;
            MessageData = messageData;
            MessageType = messageType;
        }

        public string MessageData { get; set; }
        public string MessageType { get; set; }
        public bool OperationFailed { get; set; }
        public Guid OperationId { get; set; }
        public Guid TaskId { get; set; }
        public Guid InitiatingTaskId { get; set; }

        public static OperationTaskStatusEvent Failed(Guid operationId, Guid initiatingTaskId, Guid taskId)
        {
            return new OperationTaskStatusEvent(operationId, initiatingTaskId, taskId, true, null, null);
        }

        public static OperationTaskStatusEvent Failed(Guid operationId, Guid initiatingTaskId, Guid taskId, object message)
        {
            var (data, typeName) = SerializeMessage(message);
            return new OperationTaskStatusEvent(operationId, initiatingTaskId, taskId, true, typeName, data);
        }

        public static OperationTaskStatusEvent Completed(Guid operationId, Guid initiatingTaskId, Guid taskId)
        {
            return new OperationTaskStatusEvent(operationId, initiatingTaskId, taskId, false, null, null);
        }

        public static OperationTaskStatusEvent Completed(Guid operationId, Guid initiatingTaskId, Guid taskId, object message)
        {
            var (data, typeName) = SerializeMessage(message);
            return new OperationTaskStatusEvent(operationId, initiatingTaskId,taskId, false, typeName, data);
        }

        private static (string data, string type) SerializeMessage(object message)
        {
            if (message == null)
                return (null, null);


            return (JsonSerializer.Serialize(message), message.GetType().AssemblyQualifiedName);
        }

        public object GetMessage()
        {
            if (MessageData == null || MessageType == null)
                return null;

            var type = Type.GetType(MessageType);

            return type == null 
                ? null 
                : JsonSerializer.Deserialize(MessageData, type);
        }
    }

    public class OperationTaskStatusEvent<T> : OperationTaskStatusEvent where T : class, new()
    {

        public OperationTaskStatusEvent()
        {
        }

        public OperationTaskStatusEvent(OperationTaskStatusEvent message) :
            base(message.OperationId, message.InitiatingTaskId, message.TaskId, message.OperationFailed, message.MessageType, message.MessageData)
        {
        }
    }
}