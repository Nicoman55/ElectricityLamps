using Audio;
using UnityEngine;

namespace ColoredLights_Port
{
    // UI window group controller for the custom electricity lamps menu.
    // Connects the selected TileEntityElectricityLightBlock to the stats UI,
    // handles opening and closing behavior, plays UI sounds and closes the
    // window automatically if the linked tile entity is destroyed.
    public class XUiC_ElectricityLampsWindowGroup : XUiController
    {
        private XUiC_ElectricityLampsStats ElectricityLampsStats;
        private TileEntityElectricityLightBlock tileEntity;

        // Initializes the window group and links the stats controller to this owner.
        public override void Init()
        {
            base.Init();
            XUiController childByType = base.GetChildByType<XUiC_ElectricityLampsStats>();
            bool hasStatsController = childByType != null;
            if (hasStatsController)
            {
                this.ElectricityLampsStats = (XUiC_ElectricityLampsStats)childByType;
                this.ElectricityLampsStats.Owner = this;
            }
        }

        public TileEntityElectricityLightBlock TileEntity
        {
            get
            {
                return this.tileEntity;
            }
            set
            {
                this.tileEntity = value;
                this.ElectricityLampsStats.TileEntity = this.tileEntity;
            }
        }

        // Tells the XUi system that this controller does not need constant updates.
        public override bool AlwaysUpdate()
        {
            return false;
        }

        // Opens the electricity lamps window and prepares related UI state.
        public override void OnOpen()
        {
            base.OnOpen();
            //Debug.Log("ElectricityLampsStats OnOpen | TileEntity: " + (this.TileEntity != null));
            //if (this.TileEntity != null)
            //{
            //    Debug.Log("LightRange: " + this.TileEntity.LightRange + " | LightIntensity: " + this.TileEntity.LightIntensity);
            //    Debug.Log("BlockType: " + this.TileEntity.GetChunk().GetBlock(this.TileEntity.localChunkPos).type);
            //}
            bool shouldOpenHiddenViewComponent = base.ViewComponent != null && !base.ViewComponent.IsVisible;
            if (shouldOpenHiddenViewComponent)
            {
                base.ViewComponent.OnOpen();
                base.ViewComponent.IsVisible = true;
            }
            base.xui.RecenterWindowGroup(this.windowGroup);
            for (int i = 0; i < this.children.Count; i++)
            {
                this.children[i].OnOpen();
            }
            bool canOpenStatsPanel = this.ElectricityLampsStats != null && this.TileEntity != null;
            if (canOpenStatsPanel)
            {
                this.ElectricityLampsStats.OnOpen();
            }
            bool isCompassWindowOpen = base.xui.playerUI.windowManager.IsWindowOpen("compass");
            if (isCompassWindowOpen)
            {
                base.xui.playerUI.windowManager.Close("compass");
            }
            Manager.BroadcastPlayByLocalPlayer(this.TileEntity.ToWorldPos().ToVector3() + Vector3.one * 0.5f, "open_vending");
            this.IsDirty = true;
            this.TileEntity.Destroyed += new XUiEvent_TileEntityDestroyed(this.TileEntity_Destroyed);
        }

        // Closes the electricity lamps window and restores related UI state.
        public override void OnClose()
        {
            base.OnClose();
            bool shouldReopenCompassWindow = base.xui.playerUI.windowManager.GetWindow("compass") != null &&
                                            !base.xui.playerUI.windowManager.IsWindowOpen("compass");
            if (shouldReopenCompassWindow)
            {
                base.xui.playerUI.windowManager.Open("compass", false, false, true);
            }
            Manager.BroadcastPlayByLocalPlayer(this.TileEntity.ToWorldPos().ToVector3() + Vector3.one * 0.5f, "close_vending");
            this.TileEntity.Destroyed -= new XUiEvent_TileEntityDestroyed(this.TileEntity_Destroyed);
        }

        // Closes the lamp window when the linked tile entity is destroyed.
        private void TileEntity_Destroyed(ITileEntity te)
        {
            bool isCurrentTileEntityDestroyed = this.TileEntity == te;

            if (isCurrentTileEntityDestroyed)
            {
                bool isGameManagerMissing = GameManager.Instance == null;

                if (!isGameManagerMissing)
                {
                    base.xui.playerUI.windowManager.Close("electricitylamps");
                }
            }
            else
            {
                te.Destroyed -= new XUiEvent_TileEntityDestroyed(this.TileEntity_Destroyed);
            }
        }
    }
}