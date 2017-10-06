using Nethereum.Geth;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.TransactionReceipts;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts.Managed;
using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NethereumTest
{
    public class TestClass
    {
        [Fact]
        public async Task ShouldBeAbleToDeployAContract()
        {
            var senderAddress = "0x12890d2cce102216644c59daE5baed380d84830c";
            var password = "password";
            var web3Geth = new Web3Geth();

            var unlockAccountResult = await web3Geth.Personal.UnlockAccount.SendRequestAsync(senderAddress, password, 120);
            Assert.True(unlockAccountResult);

            var abi = @"[{""constant"":false,""inputs"":[{""name"":""val"",""type"":""int256""}],""name"":""multiply"",""outputs"":[{""name"":""d"",""type"":""int256""}],""payable"":false,""type"":""function""},{""inputs"":[{""name"":""multiplier"",""type"":""int256""}],""payable"":false,""type"":""constructor""}]";
            var byteCode = "0x6060604052341561000f57600080fd5b6040516020806100d4833981016040528080519150505b60008190555b505b60988061003c6000396000f300606060405263ffffffff7c01000000000000000000000000000000000000000000000000000000006000350416631df4f1448114603c575b600080fd5b3415604657600080fd5b604f6004356061565b60405190815260200160405180910390f35b60005481025b9190505600a165627a7a72305820831b45f019f45a28f65f9c7d9f711f9cfcad404fcdee5381d69c996394e1833d0029";
            var multiplier = 7;

            var transactionHash = await web3Geth.Eth.DeployContract.SendRequestAsync(abi, byteCode, senderAddress, new HexBigInteger(290000), multiplier);
            var mineResult = await web3Geth.Miner.Start.SendRequestAsync(6);
            Assert.False(mineResult);

            var receipt = await web3Geth.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            while (receipt == null)
            {
                Thread.Sleep(5000);
                receipt = await web3Geth.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            }

            mineResult = await web3Geth.Miner.Stop.SendRequestAsync();
            Assert.True(mineResult);

            var contractAddress = receipt.ContractAddress;
            var contract = web3Geth.Eth.GetContract(abi, contractAddress);
            var multiplyFunction = contract.GetFunction("multiply");

            var result = await multiplyFunction.CallAsync<int>(7);
            Assert.Equal(49, result);
        }

        [Fact]
        public async Task ShouldMakePayment()
        {
            var senderAddress = "0x12890d2cce102216644c59daE5baed380d84830c";
            var password = "password";
            var receiverAddress = "0x13f022d72158410433cbd66f5dd8bf6d2d129924";
            //var privateKey = "0xb5b1870957d373ef0eeffecc6e4812c0fd08f554b37b233526acc331bf1544f7";
            var account = new ManagedAccount(senderAddress, password);
            var web3 = new Web3(account);
            
            //web3Geth
            var web3Geth = new Web3Geth(web3.Client);

            var transactionPolling = new TransactionReceiptPollingService(web3.TransactionManager);
            var currentBalance = await web3.Eth.GetBalance.SendRequestAsync(receiverAddress);

            await web3Geth.Miner.Start.SendRequestAsync();

            var transactionReceipt = await transactionPolling.SendRequestAsync(() => 
                web3.TransactionManager.SendTransactionAsync(account.Address, receiverAddress, /*new HexBigInteger(UnitConversion.Convert.ToWei(1))*/ new HexBigInteger(100000)));

            await web3Geth.Miner.Stop.SendRequestAsync();

            var newBalance = await web3.Eth.GetBalance.SendRequestAsync(receiverAddress);

            Assert.Equal(currentBalance.Value + 100000, newBalance.Value);
        }
    }
}
