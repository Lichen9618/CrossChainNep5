using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Nep5_demo
{
    public class Nep5_demo : SmartContract
    {
        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        private static readonly byte[] admin = Neo.SmartContract.Framework.Helper.ToScriptHash("AZZzYNez2oY2VEfqRP8i8rq2d4jduCzxnh");

        private static readonly byte[] CCMC = Neo.SmartContract.Framework.Helper.ToScriptHash("AKep3bZzjPNvJWRmEPhdFQn8c2tRhihZBL");

        private const ulong factor = 100000000;

        private const ulong totalSupply = 100000000 * factor;

        [DisplayName("decimals")]
        public static byte Decimals() => 8;

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
                    return Name();
                }
                else if (operation == "symbol")
                {
                    return Symbol();
                }
                else if (operation == "totalSupply")
                {
                    return TotalSupply();
                }
                else if (operation == "decimals")
                {
                    return Decimals();
                }
                else if (operation == "deploy")
                {
                    return Deploy();
                }
                else if (operation == "showByte")
                {
                    return (byte[])args[0];
                }
                else if (operation == "balanceOf")
                {
                    return BalanceOf((byte[])args[0]);
                }
                else if (operation == "balanceOfAdmin")
                {
                    var number = Storage.Get(admin);
                    if (number == null)
                    {
                        return 0;
                    }
                    else
                    {
                        return number;
                    }                   
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
                        long ToChainID = (long)args[2];
                        byte[] ContractAddress = (byte[])args[3];
                        string Function = (string)args[4];
                        Map<string, object> T = new Map<string, object>();
                        T["address"] = (string)args[5];
                        T["amount"] = (BigInteger)args[6];
                        object[] parameters = new object[]
                        {
                            ToChainID,
                            ContractAddress,
                            Function,
                            Neo.SmartContract.Framework.Helper.Serialize(T)
                        };
                        crossChainTransfer("CreateCrossChainTx", parameters);
                    }
                    else
                    {
                        return false;
                    }
                    return true;
                }
                else if (operation == "ProcessCrossChainTransfer")
                {
                    return ProcessCrossChainTransaction((byte[])args[0]);
                }
            }
            return false;
        }

        [DisplayName("deploy")]
        public static bool Deploy()
        {
            if (TotalSupply() != 0) return false;
            Storage.Put("totalSupply", totalSupply);
            Storage.Put(admin, totalSupply);
            Transferred(null, admin, totalSupply);
            return true;
        }

        [DisplayName("totalSupply")]
        public static BigInteger TotalSupply()
        {
            return Storage.Get("totalSupply").AsBigInteger();
        }

        [DisplayName("balanceOf")]
        public static BigInteger BalanceOf(byte[] account)
        {
            if (account.Length != 20)
                throw new InvalidOperationException("The parameter account SHOULD be 20-byte addresses.");
            return Storage.Get(account).AsBigInteger();
        }

        [DisplayName("name")]
        public static string Name() => "Nep5_demo"; //name of the token

        [DisplayName("symbol")]
        public static string Symbol() => "CCNC"; //symbol of the token

        [DisplayName("transfer")]
        private static bool transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value < 0 || from.Length != 20 || to.Length != 20) return false;
            if (value == 0 || from == to) return true;
            // from part
            var from_value = Storage.Get(from).AsBigInteger();
            if (from_value < value) return false;
            Storage.Put(from, from_value - value);
            // to part
            var to_value = Storage.Get(to).AsBigInteger();
            Storage.Put(to, value+to_value);
            Transferred(from, to, value);
            return true;
        }

        [DisplayName("supportedStandards")]
        public static string[] SupportedStandards() => new string[] { "NEP-5", "NEP-7", "NEP-10" };

        [Appcall("50f8b57cccfc4eaf635e1fae9466b650b6958a2a")]
        static extern object crossChainTransfer(string operation, params object[] args);

        [Syscall("Neo.CrossChain.ProcessCrossChainTransaction")]
        public static extern bool ProcessCrossChainTransaction(byte[] args);
    }
}
