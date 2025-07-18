// SPDX-FileCopyrightText: 2025 AirFryerBuyOneGetOneFree <airfryerbuyonegetonefree@gmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Shared._Goobstation.Factory.Filters;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._Goobstation.Factory.UI;

[GenerateTypedNameReferences]
public sealed partial class LabelFilterWindow : FancyWindow
{
    [Dependency] private readonly EntityManager _entMan = default!;

    public event Action<string>? OnSetLabel;

    public LabelFilterWindow()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);

        LabelEdit.OnTextChanged += _ => OnSetLabel?.Invoke(LabelEdit.Text);
    }

    public void SetEntity(EntityUid uid)
    {
        if (!_entMan.TryGetComponent<LabelFilterComponent>(uid, out var comp))
            return;

        var max = comp.MaxLength;
        LabelEdit.IsValid = label => label.Length < max;
        LabelEdit.Text = comp.Label;
    }
}
