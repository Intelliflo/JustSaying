﻿using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Common.Logging;

namespace JustSaying.AwsTools
{
    public class DynamoTable
    {
        private readonly DynamoDbConfig _config;
        private readonly IAmazonDynamoDB _client;
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly string[] _expectedErrorCodesForConcurrentCalls = new[] { "ResourceInUseException", "ThrottlingException" };

        public DynamoTable(DynamoDbConfig config, IAmazonDynamoDB client)
        {
            _config = config;
            _client = client;
        }

        public void CreateIfNotExist()
        {
            var request = new DescribeTableRequest { TableName = _config.TableName };
            try
            {
                _client.DescribeTable(request);
            }
            catch (ResourceNotFoundException)
            {
                CreateTable();
            }
        }
        private void CreateTable()
        {
            var request = new CreateTableRequest
                {
                    AttributeDefinitions = _config.AttributeDefinitions,
                    KeySchema = _config.KeySchema,
                    ProvisionedThroughput = _config.ProvisionedThroughput,
                    TableName = _config.TableName
                };
            try
            {
                var response = _client.CreateTable(request);
                WaitUntilTableReady(_config.TableName);
            }
            catch (AmazonDynamoDBException ex)
            {
                if (!_expectedErrorCodesForConcurrentCalls.Contains(ex.ErrorCode))
                {
                    throw;
                }
            }


        }
        private void WaitUntilTableReady(string tableName)
        {
            string status = null;
            // Let us wait until table is created. Call DescribeTable.
            do
            {
                System.Threading.Thread.Sleep(5000);
                try
                {
                    var res = _client.DescribeTable(new DescribeTableRequest
                        {
                            TableName = tableName
                        });

                    Log.InfoFormat("Dynamo Table name: {0}, status: {1}",
                                      res.Table.TableName,
                                      res.Table.TableStatus);
                    status = res.Table.TableStatus;
                }
                catch (ResourceNotFoundException)
                {
                    // DescribeTable is eventually consistent. So you might
                    // get resource not found. So we handle the potential exception.
                }
            } while (status != "ACTIVE");
        }
    }
}