using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.Numerics;

namespace Nep5_demo
{
    public class Nep5_demo : SmartContract
    {
        public delegate void deleTransfer(byte[] from, byte[] to, BigInteger value);

        public static event deleTransfer Transferred;

        public static string name() => "Cross Chain Nep5_demo Coin";

        public static string symbol() => "CCNC";

        private static readonly byte[] admin = Neo.SmartContract.Framework.Helper.ToScriptHash("123");

        private static readonly byte[] CCMC = Neo.SmartContract.Framework.Helper.ToScriptHash("234");

        private const ulong factor = 100000000;

        private const ulong totalCoin = 100000000 * factor;

        public static byte decimals()
        {
            return 8;
        }

        public static object Main(string operation, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return true;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                var callingScript = ExecutionEngine.CallingScriptHash;
                if (operation == "name")
                {
                    return name();
                }
                else if (operation == "symbol")
                {
                    return symbol();
                }
                else if (operation == "totalCoin")
                {
                    return totalCoin;
                }
                else if (operation == "decimals")
                {
                    return decimals();
                }
                else if (operation == "deploy")
                {
                    if (!Runtime.CheckWitness(admin)) return false;
                    byte[] IsDeployed = Storage.Get("IsDeployed");
                    if (IsDeployed.Length != 0) return false;
                    Storage.Put("IsDeployed", 1);
                    Storage.Put(admin, totalCoin);
                    Transferred(null, admin, totalCoin);
                }
                else if (operation == "balanceOf")
                {
                    if (args.Length != 1) return 0;
                    byte[] who = (byte[])args[0];
                    if (who.Length != 20) return false;
                    return Storage.Get(who).AsBigInteger();
                }
                else if (operation == "transfer")
                {
                    if (args.Length != 3) return false;
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return transfer(from, to, value);
                }
                else if (operation == "CreateCrossChainTransfer")
                {
                    byte[] from = (byte[])args[0];
                    BigInteger value = (BigInteger)args[1];
                    if (transfer(from, CCMC, value))
                    {
                        UInt64 ToChainID = (UInt64)args[2];
                        byte[] ContractAddress = (byte[])args[3];
                        string Function = (string)args[4];
                        object[] parameters = new object[args.Length - 3];
                        args.CopyTo(parameters, 3);
                        object[] Args = new object[] { ToChainID, ContractAddress, Function, parameters };
                        crossChainTransfer("createCrossChainTx", parameters);
                    }
                    else
                    {
                        return false;
                    }
                    return true;
                }
                else if (operation == "ProcessCrossChainTransfer")
                {
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    if (callingScript != CCMC || from != CCMC) return false;
                    transfer(from, to, value);
                    return true;
                }

            }
            return false;
        }

        private static bool transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value < 0 || from.Length != 20 || to.Length != 20) return false;
            if (value == 0 || from == to) return true;
            // from part
            BigInteger from_value = Storage.Get(from).AsBigInteger();
            if (from_value.IsZero || from_value < value) return false;
            if (from_value == value)
            {
                Storage.Delete(from);
            }
            else
            {
                Storage.Put(from, from_value - value);
            }
            //to part
            BigInteger to_value = Storage.Get(to).AsBigInteger();
            Storage.Put(to, to_value + value);
            Transferred(from, to, value);
            return true;
        }

        [Appcall("77e193f1af44a61ed3613e6e3442a0fc809bb4b8")]
        static extern object crossChainTransfer(string operation, params object[] args);
    }
}
