using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Visual representation of a Slot.cs
    /// Will highlight when can be interacted with
    /// </summary>

    public class BoardSlot : MonoBehaviour
    {
        public BoardSlotType type;
        public int x;
        public int y;

        private SpriteRenderer render;
        private Collider collide;
        private float start_alpha = 0f;
        private float current_alpha = 0f;

        private static List<BoardSlot> slot_list = new List<BoardSlot>();

        void Awake()
        {
            slot_list.Add(this);
            render = GetComponent<SpriteRenderer>();
            collide = GetComponent<Collider>();
            start_alpha = render.color.a;
            render.color = new Color(render.color.r, render.color.g, render.color.b, 0f);
        }

        private void OnDestroy()
        {
            slot_list.Remove(this);
        }

        private void Start()
        {
            if (x < Slot.x_min || x > Slot.x_max || y < Slot.y_min || y > Slot.y_max)
                Debug.LogError("Board Slot X and Y value must be within the min and max set for those values, check Slot.cs script to change those min/max.");
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            BoardCard bcard_selected = PlayerControls.Get().GetSelected();
            HandCard drag_card = HandCard.GetDrag();

            Game gdata = GameClient.Get().GetGameData();
            Player player = GameClient.Get().GetPlayer();
            Slot slot = GetSlot();
            Card dcard = drag_card?.GetCard();
            Card slot_card = gdata.GetSlotCard(GetSlot());
            bool your_turn = GameClient.Get().IsYourTurn();
            collide.enabled = slot_card == null; //Disable collider when a card is here

            //Find target opacity value
            float target_alpha = 0f;
            if (your_turn && dcard != null && dcard.CardData.IsBoardCard() && gdata.CanPlayCard(dcard, slot))
            {
                target_alpha = 1f; //hightlight when dragging a character or artifact
            }

            if (your_turn && dcard != null && dcard.CardData.IsRequireTarget() && gdata.CanPlayCard(dcard, slot))
            {
                target_alpha = 1f; //Highlight when dragin a spell with target
            }

            if (gdata.selector == SelectorType.SelectTarget && player.player_id == gdata.selector_player)
            {
                Card caster = gdata.GetCard(gdata.selector_caster_uid);
                AbilityData ability = AbilityData.Get(gdata.selector_ability_id);
                if(ability != null && slot_card == null && ability.CanTarget(gdata, caster, slot))
                    target_alpha = 1f; //Highlight when selecting a target and empty slots are valid
            }

            Card select_card = bcard_selected?.GetCard();
            bool can_do_move = your_turn && select_card != null && slot_card == null && gdata.CanMoveCard(select_card, slot);
            bool can_do_attack = your_turn && select_card != null && slot_card != null && gdata.CanAttackTarget(select_card, slot_card);

            if (can_do_attack || can_do_move)
            {
                target_alpha = 1f;
            }

            //Update color and alpha
            current_alpha = Mathf.MoveTowards(current_alpha, target_alpha * start_alpha, 2f * Time.deltaTime);
            render.color = new Color(render.color.r, render.color.g, render.color.b, current_alpha);
        }

        //Find the actual slot coordinates of this board slot
        public Slot GetSlot()
        {
            int p = 0;

            if (type == BoardSlotType.FlipX)
            {
                int pid = GameClient.Get().GetPlayerID();
                int px = x;
                if ((pid % 2) == 1)
                    px = Slot.x_max - x + Slot.x_min; //Flip X coordinate if not the first player
                return new Slot(px, y, p);
            }

            if (type == BoardSlotType.FlipY)
            {
                int pid = GameClient.Get().GetPlayerID();
                int py = y;
                if ((pid % 2) == 1)
                    py = Slot.y_max - y + Slot.y_min; //Flip Y coordinate if not the first player
                return new Slot(x, py, p);
            }

            if (type == BoardSlotType.PlayerSelf)
                p = GameClient.Get().GetPlayerID();
            if(type == BoardSlotType.PlayerOpponent)
                p = GameClient.Get().GetOpponentPlayerID();
           
            return new Slot(x, y, p);
        }

        //When clicking on the slot
        public void OnMouseDown()
        {
            if (GameUI.IsOverUI())
                return;

            Game gdata = GameClient.Get().GetGameData();
            int player_id = GameClient.Get().GetPlayerID();

            if (gdata.selector == SelectorType.SelectTarget && player_id == gdata.selector_player)
            {
                Slot slot = GetSlot();
                Card slot_card = gdata.GetSlotCard(slot);
                if (slot_card == null)
                {
                    GameClient.Get().SelectSlot(slot);
                }
            }
        }

        public static BoardSlot GetNearest(Vector3 pos, float range = 999f)
        {
            BoardSlot nearest = null;
            float min_dist = range;
            foreach (BoardSlot slot in GetAll())
            {
                float dist = (slot.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = slot;
                }
            }
            return nearest;
        }

        public static BoardSlot Get(Slot slot)
        {
            return Get(slot.x, slot.y, slot.p);
        }

        public static BoardSlot Get(int x, int y, int p)
        {
            foreach (BoardSlot bslot in GetAll())
            {
                Slot slot = bslot.GetSlot();
                if (slot.x == x && slot.y == y && slot.p == p)
                    return bslot;
            }
            return null;
        }

        public static BoardSlot Get(int x, int y)
        {
            foreach (BoardSlot bslot in GetAll())
            {
                Slot slot = bslot.GetSlot();
                if (slot.x == x && slot.y == y)
                    return bslot;
            }
            return null;
        }

        public static List<BoardSlot> GetAll()
        {
            return slot_list;
        }
    }

    public enum BoardSlotType
    {
        Fixed = 0,              //x,y,p = slot
        PlayerSelf = 5,         //p = client player id
        PlayerOpponent = 7,     //p = client's opponent player id
        FlipX = 10,              //p=0,   x=unchanged for first player,  x=reversed for second player
        FlipY = 11,              //p=0,   y=unchanged for first player,  y=reversed for second player
    }
}