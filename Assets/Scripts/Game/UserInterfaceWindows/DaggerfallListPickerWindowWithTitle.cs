using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using UnityEngine;

public class DaggerfallListPickerWindowWithTitle : DaggerfallListPickerWindow
{
    private string pickerWindowTitle;
    TextLabel pickerWindowTitleLabel;

    public DaggerfallListPickerWindowWithTitle(IUserInterfaceManager uiManager, string title,
        IUserInterfaceWindow previous = null, DaggerfallFont font = null, int rowsDisplayed = 0)
        : base(uiManager, previous, font, rowsDisplayed)
    {
        pickerWindowTitle = title;
    }

    protected override void Setup()
    {
        base.Setup();
        pickerWindowTitleLabel = new TextLabel();
        pickerWindowTitleLabel.Text = pickerWindowTitle;
        pickerWindowTitleLabel.Position = new Vector2(
            Mathf.Round((pickerPanel.Size.x - pickerWindowTitleLabel.Size.x) * 0.5f), 3);
        pickerPanel.Components.Add(pickerWindowTitleLabel);
    }

    public void ChangeTitle(string title)
    {
        if (!IsSetup)
            Setup();
        pickerWindowTitleLabel.Text = title;
        pickerWindowTitleLabel.Position = new Vector2(
            Mathf.Round((pickerPanel.Size.x - pickerWindowTitleLabel.Size.x) * 0.5f), 3);
    }
}
