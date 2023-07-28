using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;
using UnityEngine.Events;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// 包含用於點擊卡牌、攻擊、激活能力的主要控件的腳本
    /// 保存當前選定的卡牌，並在單擊釋放時將操作發送到 GameClient
    /// </summary>

    public class PlayerControls : MonoBehaviour
    {
        private BoardCard selected_card = null;

        private static PlayerControls instance;

        void Awake()
        {
            instance = this;
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            if (Input.GetMouseButtonDown(1))
                UnselectAll();

            if (selected_card != null)
            {
                if (Input.GetMouseButtonUp(0))
                    ReleaseClick();
            }
        }

        public void SelectCard(BoardCard bcard)
        {
            int player_id = GameClient.Get().GetPlayerID();
            bool yourturn = GameClient.Get().IsYourTurn();
            bool yourcard = bcard.GetCard().player_id == player_id;
            Game gdata = GameClient.Get().GetGameData();

            if (gdata.selector == SelectorType.SelectTarget && player_id == gdata.selector_player)
            {
                //目標選擇器，選擇這張卡
                GameClient.Get().SelectCard(bcard.GetCard());
            }
            else if (gdata.state == GameState.Play && gdata.selector == SelectorType.None && yourcard && yourturn)
            {
                //開始拖動卡片
                selected_card = bcard;
            }
        }

        public void SelectCardRight(BoardCard card)
        {
            if (!Input.GetMouseButton(0))
            {
                //右鍵什麼也沒有
            }
        }

        private void ReleaseClick()
        {
            bool yourturn = GameClient.Get().IsYourTurn();
            Game gdata = GameClient.Get().GetGameData();

            if (yourturn && selected_card != null)
            {
                Vector3 wpos = GameBoard.Get().RaycastMouseBoard();
                BoardSlot tslot = BoardSlot.GetNearest(wpos, 2f);
                Card target = tslot ? gdata.GetSlotCard(tslot.GetSlot()) : null;
                AbilityButton ability = AbilityButton.GetHover(wpos, 1f);
                BoardSlotPlayer zone = BoardSlotPlayer.Get(true);
                
                if (ability != null && ability.IsVisible())
                {
                    ability.OnClick();
                }
                else if (zone.IsInRange(wpos, 3f, 1f))
                {
                    if (selected_card.GetCard().exhausted)
                        WarningText.ShowExhausted();
                    else
                        GameClient.Get().AttackPlayer(selected_card.GetCard(), zone.GetPlayer());
                }
                else if (target != null && target.uid != selected_card.GetCardUID())
                {
                    if(selected_card.GetCard().exhausted)
                        WarningText.ShowExhausted();
                    else
                        GameClient.Get().AttackTarget(selected_card.GetCard(), target);
                }
                else if (tslot != null)
                {
                    GameClient.Get().Move(selected_card.GetCard(), tslot.GetSlot());
                }
            }

            UnselectAll();
        }

        public void UnselectAll()
        {
            selected_card = null;
        }

        public BoardCard GetSelected()
        {
            return selected_card;
        }

        public static PlayerControls Get()
        {
            return instance;
        }
    }
}