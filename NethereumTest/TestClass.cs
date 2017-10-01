using Nethereum.Geth;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
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
            var byteCode = "0x6060604052341561000f57600080fd5b6040516020806100d4833981016040528080519150505b60008190555b505b60988061003c6000396000f300606060405263ffffffff7c01000000000000000000000000000000000000000000000000000000006000350416631df4f1448114603c575b600080fd5b3415604657600080fd5b604f6004356061565b60405190815260200160405180910390f35b60005481025b9190505600a165627a7a72305820ecfd6d622f21625dde33f4f523ec0fb3571394fbf02215202b80150d32d74ad80029";
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
    }
}
