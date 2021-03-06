﻿using ETWController.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace ETWController.Network
{
    /// <summary>
    /// Host the trace service locally
    /// </summary>
    class SelfHostedService
    {
        EndpointAddress RemoteEndpointAddress;


        /// <summary>
        /// Host for an in process hosted WCF web service.
        /// </summary>
        public SelfHostedService()
        {
        }

        /// <summary>
        /// Host a service at given uri
        /// </summary>
        /// <param name="url"></param>
        public SelfHostedService(string remoteUrl)
        {
            RemoteEndpointAddress = new EndpointAddress(new Uri(remoteUrl));
        }

        /// <summary>
        /// Start hosting the service
        /// </summary>
        /// <param name="locaUurl">Url under which the web service should be reachable. E.g. http://localhost:8091/TraceControlerService </param>
        /// <returns></returns>
        public ServiceHost HostService(string localUrl) 
        {
            var localEndpointAddress = new EndpointAddress(new Uri(localUrl));
            var hostedService = new ServiceHost(typeof(TraceControlerService), localEndpointAddress.Uri);

            hostedService.AddServiceEndpoint(typeof(ITraceControlerService), CreateBinding(), "");
            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            smb.HttpGetEnabled = true;
            hostedService.Description.Behaviors.Add(smb);
            //Start the Service
            hostedService.Open();

            return hostedService;
        }

        /// <summary>
        /// Call method of remote service where the client channel is created and disposed
        /// at every call to ensure a maximum of safety to reliable call onto the server. 
        /// </summary>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="code"></param>
        /// <returns></returns>
        public TReturn UseService<TReturn>(Func<ITraceControlerService, TReturn> code)
        {
            var channel = ChannelFactory<ITraceControlerService>.CreateChannel(CreateBinding(), RemoteEndpointAddress);
            bool error = true;
            try
            {
                var clientChanel = (IClientChannel)channel;
                clientChanel.OperationTimeout = TimeSpan.FromMinutes(10);
                TReturn result = code(channel);
                
                clientChanel.Close();
                error = false;
                return result;
            }
            catch(ProtocolException ex)
            {
                throw new ArgumentException("Wrong protocol detected. The most likely reason is that the WCF port runs already a different service not related to ETWController.", ex);
            }
            finally
            {
                if (error)
                {
                    ((IClientChannel)channel).Abort();
                }
            }
        }

        /// <summary>
        /// Call method of remote service where the client channel is created and disposed
        /// at every call to ensure a maximum of safety to reliable call onto the server. 
        /// </summary>
        /// <param name="code"></param>
        public void UseService(Action<ITraceControlerService> code)
        {
            var channel = ChannelFactory<ITraceControlerService>.CreateChannel(CreateBinding(), RemoteEndpointAddress);
            bool error = true;
            try
            {
                code(channel);
                ((IClientChannel)channel).Close();
                error = false;
            }
            finally
            {
                if (error)
                {
                    ((IClientChannel)channel).Abort();
                }
            }
        }

        /// <summary>
        /// Default is wshttp with a 10 minute timeout to ensure that stopping the 
        /// tracing which created the NGen images does not end prematurely due to a 
        /// network timeout. 
        /// </summary>
        /// <returns></returns>
        static Binding CreateBinding()
        {
          //  var binding = new BasicHttpBinding(BasicHttpSecurityMode.None);
            var binding = new WSHttpBinding(SecurityMode.None);
            binding.ReceiveTimeout = TimeSpan.FromMinutes(10);
            binding.SendTimeout = TimeSpan.FromMinutes(10);
            return binding;
        }
    }
}
