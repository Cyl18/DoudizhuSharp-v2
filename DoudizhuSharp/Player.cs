using System;
using System.Collections.Generic;
using System.Text;
using DoudizhuSharp.Messages;

namespace DoudizhuSharp
{
    public class Player
    {
        public List<Card> Cards { get; } = new List<Card>();
        public string PlayerID { get; }
        public TargetSender Sender { get; }
        public Role Role { get; internal set; } = Role.Farmer;
        public Player(string playerID)
        {
            PlayerID = playerID;
            Sender = playerID.GetPrivateSender();
        }

        public void SendCard(ICollection<Card> cards)
        {
            Cards.AddRange(cards);
            Cards.Sort();
            SendCardMessage();
        }

        private void SendCardMessage()
        {
            Sender.Send($"你现在的牌有{Cards.ToCardString()}");
        }

        public void RemoveCard(ICollection<Card> cards)
        {
            foreach (var card in cards)
            {
                Cards.Remove(card);
            }
            SendCardMessage();
        }
    }
    public static class PlayerExtensions
    {
        public static string ToAtCode(this Player player)
        {
            return player.PlayerID.ToAtCode();
        }

        public static string ToAtCode(this string playerID)
        {
            return MessageCore.AtCoder(playerID);
        }
    }
    public enum Role
    {
        Farmer,
        Landlord
    }
}
