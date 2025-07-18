// SPDX-FileCopyrightText: 2024 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 gluesniffler <linebarrelerenthusiast@gmail.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Client._Shitmed.Xenonids.UI;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._Shitmed.Medical.Surgery;

[GenerateTypedNameReferences]
public sealed partial class SurgeryStepButton : XenoChoiceControl
{
    public EntityUid Step { get; set; }

    public SurgeryStepButton()
    {
        RobustXamlLoader.Load(this);
    }
}
