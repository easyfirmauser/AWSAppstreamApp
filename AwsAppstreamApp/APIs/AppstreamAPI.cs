using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.AppStream;
using Amazon.AppStream.Model;

namespace AWSAppstreamApp.APIs
{
    //<copyright file="AppstreamAPI.cs" company="WoAx-IT Wolfgang Axamit KG">
    // WoAx-IT Wolfgang Axamit KG. All rights reserved.
    // </copyright>  
    public static class AppstreamAPI
    {
        private static bool initHappened;
        private static string AccessKeyID;
        private static string AccessKeySecret;

        public static void Init(string pAccessKeyId, string pAccessKeySecret)
        {
            AccessKeyID = pAccessKeyId;
            AccessKeySecret = pAccessKeySecret;
            initHappened = true;
        }
        private static AmazonAppStreamClient GetAwsAppstreamClient()
        {
            var vClient = new AmazonAppStreamClient(AccessKeyID,
                AccessKeySecret,
                RegionEndpoint.EUCentral1);
            return vClient;
        }
        public static async Task<List<User>> GetUsers()
        {
            AmazonAppStreamClient vClient = GetAwsAppstreamClient();
            var vList = await vClient?.DescribeUsersAsync(new DescribeUsersRequest()
            {
                AuthenticationType = AuthenticationType.USERPOOL
            });
            return vList?.Users;
        }

        public static async Task<List<UserStackAssociation>> GetUserStackAssociations(List<Stack> pStacks)
        {
            AmazonAppStreamClient vClient = GetAwsAppstreamClient();
            var vRes = new List<UserStackAssociation>();
            if (pStacks != null)
                foreach (var vStack in pStacks)
                {
                    var vList = await vClient?.DescribeUserStackAssociationsAsync(
                        new DescribeUserStackAssociationsRequest()
                        {
                            AuthenticationType = AuthenticationType.USERPOOL,
                            StackName = vStack.Name
                        });
                    if (vList?.UserStackAssociations != null) 
                        vRes.AddRange(vList?.UserStackAssociations);
                }
            return vRes;
        }

        public static string DeleteUser(string pUserName)
        {
            var vClient = GetAwsAppstreamClient();

            var vDeleteRes = vClient.DeleteUser(
                new DeleteUserRequest
                {
                    UserName = pUserName,
                    AuthenticationType = AuthenticationType.USERPOOL
                });

            return vDeleteRes.HttpStatusCode.ToString();
        }
        public static string DisableUser(string pUserName)
        {
            var vClient = GetAwsAppstreamClient();

            var vRes = vClient.DisableUser(
                new DisableUserRequest
                {
                    UserName = pUserName,
                    AuthenticationType = AuthenticationType.USERPOOL
                });

            return vRes.HttpStatusCode.ToString();
        }
        public static string EnableUser(string pUserName)
        {
            var vClient = GetAwsAppstreamClient();

            var vRes = vClient.EnableUser(
                new EnableUserRequest()
                {
                    UserName = pUserName,
                    AuthenticationType = AuthenticationType.USERPOOL
                });

            return vRes.HttpStatusCode.ToString();
        }
        public static string UserToStack(string pUserName, string pStackname)
        {
            var vClient = GetAwsAppstreamClient();

            var vRes = vClient.BatchAssociateUserStack(new BatchAssociateUserStackRequest()
            {
                UserStackAssociations = new List<UserStackAssociation>()
                {
                    new UserStackAssociation()
                    {
                        UserName = pUserName,
                        StackName = pStackname,
                        AuthenticationType = AuthenticationType.USERPOOL,
                        SendEmailNotification = true
                    }
                }
            });
            return vRes.HttpStatusCode.ToString();
        }
        public static string DeleteUserToStack(string pUserName, string pStackname)
        {
            var vClient = GetAwsAppstreamClient();
            var vRes = vClient.BatchDisassociateUserStack(
                new BatchDisassociateUserStackRequest()
            {
                UserStackAssociations = new List<UserStackAssociation>()
                {
                    new UserStackAssociation()
                    {
                        UserName = pUserName,
                        StackName = pStackname,
                        AuthenticationType = AuthenticationType.USERPOOL,
                        SendEmailNotification = true
                    }
                }
            });

            return vRes.HttpStatusCode.ToString();
        }
        public static string CreateUser(string pFirstName, string pLastName, string pUserName)
        {
            var vClient = GetAwsAppstreamClient();
            var vReq = new CreateUserRequest()
            {
                AuthenticationType = AuthenticationType.USERPOOL,
                FirstName = String.IsNullOrWhiteSpace(pFirstName) ? null : pFirstName,
                LastName = String.IsNullOrWhiteSpace(pLastName) ? null : pLastName,
                UserName = pUserName
            };

            var vUserResponse = vClient.CreateUser(vReq);

            return vUserResponse.HttpStatusCode.ToString();
        }
        public static async Task<List<Stack>> GetStacks()
        {
            var vClient = GetAwsAppstreamClient();
            var vStacks = await vClient?.DescribeStacksAsync(new DescribeStacksRequest());
            return vStacks?.Stacks;
        }
        public static async Task<List<Fleet>> GetFleets()
        {
            var vClient = GetAwsAppstreamClient();
            var vFleets = await vClient?.DescribeFleetsAsync(new DescribeFleetsRequest());
            return vFleets?.Fleets;
        }
        
    }
}
