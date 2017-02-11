﻿namespace Binding
{
    using System;
    using System.Data;
    using System.Linq;
    using Helper;
    using Items;

    public class KeyBindingWriterEliteDangerous : IKeyBindingWriter
    {
        //Initialise ..
        private const string XMLRoot = "Root";
        private const string XMLPresetName = "PresetName";
        private const string XMLKey = "Key";
        private const string XMLDevice = "Device";
        private const string XMLModifier = "Modifier";

        /// <summary>
        /// Update vacant Elite Dangerous Name Action binding with Key derived from Voice Attack Profile ..
        /// </summary>
        /// <param name="reverseBindableVacantEDActions"></param>
        /// <param name="updateChangeTag"></param>
        /// <returns></returns>
        public bool Update(DataTable reverseBindableVacantEDActions, bool updateChangeTag)
        {
            // Initialise ..
            bool bindsUpdated = false;
            string globalEliteDangerousInternal = string.Empty;
            string globalVoiceAttackProfileFilePath = string.Empty;

            // Find Elite Dangerous commands which are vacant and available for remapping ..
            var vacantBindings = from vb in reverseBindableVacantEDActions.AsEnumerable()
                                       select
                                          new
                                          {
                                              KeyEnumeration = vb.Field<string>(Edvard.Column.KeyEnumeration.ToString()),
                                              EliteDangerousAction = vb.Field<string>(Edvard.Column.EliteDangerousAction.ToString()),
                                              VoiceAttackAction = vb.Field<string>(Edvard.Column.VoiceAttackAction.ToString()),
                                              VoiceAttackKeyValue = vb.Field<string>(Edvard.Column.VoiceAttackKeyValue.ToString()),
                                              VoiceAttackKeyCode = vb.Field<string>(Edvard.Column.VoiceAttackKeyCode.ToString()),
                                              EliteDangerousKeyValue = vb.Field<string>(Edvard.Column.EliteDangerousKeyValue.ToString()),
                                              VoiceAttackModifierKeyValue = vb.Field<string>(Edvard.Column.VoiceAttackModifierKeyValue.ToString()),
                                              VoiceAttackModifierKeyCode = vb.Field<string>(Edvard.Column.VoiceAttackModifierKeyCode.ToString()),
                                              EliteDangerousModifierKeyValue = vb.Field<string>(Edvard.Column.EliteDangerousModifierKeyValue.ToString()),
                                              VoiceAttackInternal = vb.Field<string>(Edvard.Column.VoiceAttackInternal.ToString()),
                                              VoiceAttackProfile = vb.Field<string>(Edvard.Column.VoiceAttackProfile.ToString()),
                                              EliteDangerousInternal = vb.Field<string>(Edvard.Column.EliteDangerousInternal.ToString()),
                                              EliteDangerousBinds = vb.Field<string>(Edvard.Column.EliteDangerousBinds.ToString()),
                                          };

            // Process each potentially vacant binding ..
            foreach (var vacantBinding in vacantBindings)
            {
                bool updateStatus = false;

                // Try to update Primary bind ..
                updateStatus = this.UpdateVacantEliteDangerousBinding(vacantBinding.EliteDangerousBinds, 
                                                                      Application.EliteDangerousDevicePriority.Primary.ToString(), 
                                                                      vacantBinding.EliteDangerousAction, 
                                                                      vacantBinding.EliteDangerousKeyValue);

                // If Primary bind attempt fails, try to update Secondary bind ..
                if (!updateStatus)
                {
                    updateStatus = this.UpdateVacantEliteDangerousBinding(vacantBinding.EliteDangerousBinds,
                                                                          Application.EliteDangerousDevicePriority.Secondary.ToString(),
                                                                          vacantBinding.EliteDangerousAction,
                                                                          vacantBinding.EliteDangerousKeyValue);
                }
                
                if (updateStatus)
                {
                    globalEliteDangerousInternal = vacantBinding.EliteDangerousInternal;
                    globalVoiceAttackProfileFilePath = vacantBinding.EliteDangerousBinds;
                    bindsUpdated = true;
                }
            }

            // Update internal reference ..
            if (bindsUpdated && updateChangeTag)
            {
                this.UpdateBindsPresetName(globalVoiceAttackProfileFilePath, globalEliteDangerousInternal, Tag.Make(globalEliteDangerousInternal));
            }

            return bindsUpdated;
        }

        /// <summary>
        /// Update Key Code associated to specific [Id] in Voice Attack when Action has not been bound ..
        /// </summary>
        /// <remarks>
        ///   Format: XML
        ///             o <Root/>
        ///               |_ <KeyboardLayout/>
        ///               |_ <things></things>.[Value] attribute
        ///               |_ <things/>
        ///                  |_<Binding/>
        ///                  |_<Inverted/>
        ///                  |_<Deadzone/>
        ///               |_ <things/>
        ///                  |_<Primary/>
        ///                     |_<Device = {NoDevice}/>[*]
        ///                     |_<Key/ = empty>[*]
        ///                  |_<Secondary/>
        ///                     |_<Device/>
        ///                     |_<Key/>
        /// </remarks>
        /// <param name="edbinds"></param>
        /// <param name="devicePriority"></param>
        /// <param name="actionName"></param>
        /// <param name="keyvalue"></param>
        /// <returns></returns>
        private bool UpdateVacantEliteDangerousBinding(string edbinds, string devicePriority, string actionName, string keyvalue)
        {
            // Initialise ..
            const string VacantDeviceIndicator = "{NoDevice}";
            bool success = false;

            var edb = HandleXml.ReadXDoc(edbinds);

            // Check to see if Key_value already set on primary binding for Action (no need to set same binding on secondary) ..
            var primaryKeyBindingIsSet = edb.Descendants(Application.EliteDangerousDevicePriority.Primary.ToString())
                                            .Where(item => item.Parent.SafeElementName() == actionName &&
                                                   item.SafeElementName() == Application.EliteDangerousDevicePriority.Primary.ToString() &&
                                                   item.SafeAttributeValue(XMLDevice) == Application.Interaction.Keyboard.ToString() &&
                                                   item.SafeAttributeValue(XMLKey) == Application.EliteDangerousBindingPrefix.Key_.ToString() + keyvalue).FirstOrDefault();

            // If not, attempt binding update ..
            if (primaryKeyBindingIsSet == null)
            {
                try
                {
                    // Update [Key Binding] for Elite Dangerous Action using Key Value  ..
                    edb.Descendants(devicePriority)
                       .Where(item => item.Parent.SafeElementName() == actionName &&
                              item.SafeElementName() == devicePriority &&
                              item.SafeAttributeValue(XMLDevice) == VacantDeviceIndicator &&
                              item.SafeAttributeValue(XMLKey) == string.Empty).FirstOrDefault()
                       .SetAttributeValue(XMLKey, Application.EliteDangerousBindingPrefix.Key_.ToString() + keyvalue);

                    // Update [Device Type] for Elite Dangerous Action (must always follow key-binding update) ..
                    edb.Descendants(devicePriority)
                       .Where(item => item.Parent.SafeElementName() == actionName &&
                              item.SafeElementName() == devicePriority &&
                              item.SafeAttributeValue(XMLDevice) == VacantDeviceIndicator &&
                              item.SafeAttributeValue(XMLKey) == Application.EliteDangerousBindingPrefix.Key_.ToString() + keyvalue).FirstOrDefault()
                       .SetAttributeValue(XMLDevice, Application.Interaction.Keyboard.ToString());

                    edb.Save(edbinds);

                    success = true;
                }
                catch (Exception)
                {
                    success = false;
                }
            }

            return success;
        }

        /// <summary>
        /// Update Elite Dangerous Binds Preset Name
        /// </summary>
        /// <remarks>
        ///   Format: XML
        ///             o <Root PresetName=[*]/>
        ///               |_ <KeyboardLayout/>
        ///               |_ <things/>
        ///                  |_<Binding/>
        ///                  |_<Inverted/>
        ///                  |_<Deadzone/>
        ///               |_ <things/>
        ///                  |_<Primary/>
        ///                     |_<Device = {NoDevice}/>
        ///                     |_<Key/ = empty>
        /// </remarks>
        /// <param name="vaprofile"></param>
        /// <param name="profileName"></param>
        /// <param name="updatedProfileName"></param>
        private void UpdateBindsPresetName(string edbinds, string presetName, string updatedPresetName)
        {
            var edb = HandleXml.ReadXDoc(edbinds);
 
            // Update attribute of root node ..
            edb.Root
               .Attributes(XMLPresetName)
               .Where(item => item.Value == presetName).FirstOrDefault()
               .SetValue(updatedPresetName);

            edb.Save(edbinds);
        }
    }
}