﻿namespace Bindings
{
    using System;
    using System.Data;
    using Helpers;

    /// <summary>
    /// Analyse Actions between VoiceAttack and Elite Dangerous ..
    /// </summary>
    public static class GameBindingsAnalyser
    {
        /// <summary>
        /// Consolidate Command Key Codes in VoiceAttack based on Elite Dangerous Binds as Master ..
        /// </summary>
        /// <param name="voiceAttack"></param>
        /// <param name="eliteDangerous"></param>
        /// <returns></returns>
        public static DataTable ForUpdateInVoiceAttack(DataTable voiceAttack, DataTable eliteDangerous)
        {
            // Initialise lookup dictionary for inter-game action references ..
            CommandExchange actions = new CommandExchange();
            actions.Initialise();

            // Datatable to hold tabulated contents ..
            DataTable consolidatedaction = TableShape.ConsolidatedActions();

            // Search through all defined Voice Attack bindings ..
            var voiceattackBindings = from va in voiceAttack.AsEnumerable()
                                    select
                                       new
                                         {
                                            KeyEnumeration = va.Field<string>(Enums.Column.KeyEnumeration.ToString()),
                                            EliteDangerousAction = actions.GetED(va.Field<string>(Enums.Column.KeyAction.ToString())),
                                            Action = va.Field<string>(Enums.Column.KeyAction.ToString()),
                                            KeyValue = va.Field<string>(Enums.Column.KeyEnumerationValue.ToString()),
                                            KeyCode = va.Field<int>(Enums.Column.KeyEnumerationCode.ToString()),
                                            KeyID = va.Field<string>(Enums.Column.KeyId.ToString()),
                                            ModifierKeyGameValue = va.Field<string>(Enums.Column.ModifierKeyGameValue.ToString()),
                                            ModifierKeyEnumerationValue = va.Field<string>(Enums.Column.ModifierKeyEnumerationValue.ToString()),
                                            ModifierKeyEnumerationCode = va.Field<int>(Enums.Column.ModifierKeyEnumerationCode.ToString()),
                                            ModifierKeyID = va.Field<string>(Enums.Column.ModifierKeyId.ToString()),
                                            FilePath = va.Field<string>(Enums.Column.FilePath.ToString()),
                                            Internal = va.Field<string>(Enums.Column.Internal.ToString())
                                         };

            // .. and compare with what has been defined in the Elite Dangerous bindings ..
            foreach (var voiceattackBinding in voiceattackBindings)
            {
                bool commandDefinedInEliteDangerousBindsFile = false;
                string remapRequired = "unknown";
                string rationale = "unknown";

                var elitedangerousBindings = from ed in eliteDangerous.AsEnumerable()
                                            where ed.Field<string>(Enums.Column.KeyAction.ToString()) == voiceattackBinding.EliteDangerousAction
                                           select
                                              new
                                                {
                                                    Action = ed.Field<string>(Enums.Column.KeyAction.ToString()),
                                                    KeyPriority = ed.Field<string>(Enums.Column.DevicePriority.ToString()),
                                                    KeyGameValue = ed.Field<string>(Enums.Column.KeyGameValue.ToString()),
                                                    KeyEnumerationValue = ed.Field<string>(Enums.Column.KeyEnumerationValue.ToString()),
                                                    KeyEnumerationCode = ed.Field<int>(Enums.Column.KeyEnumerationCode.ToString()),
                                                    KeyID = ed.Field<string>(Enums.Column.KeyId.ToString()),
                                                    ModifierKeyGameValue = ed.Field<string>(Enums.Column.ModifierKeyGameValue.ToString()),
                                                    ModifierKeyEnumerationValue = ed.Field<string>(Enums.Column.ModifierKeyEnumerationValue.ToString()),
                                                    ModifierKeyEnumerationCode = ed.Field<int>(Enums.Column.ModifierKeyEnumerationCode.ToString()),
                                                    ModifierKeyID = ed.Field<string>(Enums.Column.ModifierKeyId.ToString()),
                                                    FilePath = ed.Field<string>(Enums.Column.FilePath.ToString()),
                                                    Internal = ed.Field<string>(Enums.Column.Internal.ToString())
                                                };

                // Compare matching action bindings with their assigned key value/code to evaluate which key code(s) require remapping ..
                foreach (var elitedangerousBinding in elitedangerousBindings)
                {
                    commandDefinedInEliteDangerousBindsFile = true;

                    // Check for: satisfactory alignment of regular and modifier key codes ..
                    if (
                        //// Matching Regular Key Codes with no Modifier Key(s) present ..
                        ((elitedangerousBinding.KeyEnumerationCode == voiceattackBinding.KeyCode) &&
                        (elitedangerousBinding.ModifierKeyEnumerationCode == StatusCode.NotApplicableInt))     ||
                        
                        //// Matching Regular Key Codes with matching Modifier Key(s) present ..
                        ((elitedangerousBinding.KeyEnumerationCode == voiceattackBinding.KeyCode) &&
                        (elitedangerousBinding.ModifierKeyEnumerationCode == voiceattackBinding.ModifierKeyEnumerationCode)))
                    {
                        remapRequired = Enums.KeyUpdateRequired.NO.ToString();
                        rationale = "ED o--o VA Key Codes are aligned";
                    }
                    else
                    {
                        rationale = string.Empty;

                        // Check for misaligned codes ..
                        if (elitedangerousBinding.KeyEnumerationCode > StatusCode.EmptyStringInt)
                        {
                            remapRequired = Enums.KeyUpdateRequired.YES_ed_to_va.ToString();

                            // Check for: misaligned key codes ..
                            if (elitedangerousBinding.KeyEnumerationCode != voiceattackBinding.KeyCode)
                            {
                                rationale += string.Format("Misaligned key codes: ED[{0}] o--O VA[{1}];", elitedangerousBinding.KeyEnumerationCode, voiceattackBinding.KeyCode);
                            }

                            // Check for: misaligned modifier key codes ..
                            if (elitedangerousBinding.ModifierKeyEnumerationCode != voiceattackBinding.ModifierKeyEnumerationCode)
                            {
                                if (voiceattackBinding.ModifierKeyEnumerationCode == StatusCode.EmptyStringInt)
                                {
                                    rationale += string.Format("Misaligned modifier key codes: ED[{0}] o--O VA[{1}];", elitedangerousBinding.ModifierKeyEnumerationCode, "no-modifier defined");
                                }
                                else
                                {
                                    rationale += string.Format("Misaligned modifier key codes: ED[{0}] o--O VA[{1}];", elitedangerousBinding.ModifierKeyEnumerationCode, voiceattackBinding.ModifierKeyEnumerationCode);
                                }
                            }
                        }

                        // Check for unresolvable key codes ..
                        if (elitedangerousBinding.KeyEnumerationCode == StatusCode.NoEquivalentKeyFoundAtExchange || elitedangerousBinding.KeyEnumerationCode == StatusCode.NoCodeFoundAfterExchange)
                        {
                            remapRequired = Enums.KeyUpdateRequired.NO.ToString();
                            rationale += string.Format("Unresolvable key code for: ED[{0}];", elitedangerousBinding.KeyGameValue);
                        }

                        // Check for: unresolvable modifier key codes ..
                        if (elitedangerousBinding.ModifierKeyEnumerationCode == StatusCode.NoEquivalentKeyFoundAtExchange || elitedangerousBinding.ModifierKeyEnumerationCode == StatusCode.NoCodeFoundAfterExchange)
                        {
                            remapRequired = Enums.KeyUpdateRequired.NO.ToString();
                            rationale += string.Format("Unresolvable modifier key code for: ED[{0}];", elitedangerousBinding.ModifierKeyGameValue);
                        }
                    }

                    // Append evaluated results to DataTable ..
                    consolidatedaction.LoadDataRow(new object[] 
                                                {
                                                 ////--------------------------------------------------------------------------
                                                 voiceattackBinding.KeyEnumeration, //KeyEnumeration
                                                 ////--------------------------------------------------------------------------
                                                 remapRequired, //ReMapRequired
                                                 rationale, //Rationale
                                                 ////--------------------------------------------------------------------------
                                                 voiceattackBinding.Action, //VoiceAttackAction
                                                 voiceattackBinding.EliteDangerousAction, //EliteDangerousAction
                                                 ////--------------------------------------------------------------------------
                                                 voiceattackBinding.KeyValue, //VoiceAttackKeyValue
                                                 voiceattackBinding.KeyCode, //VoiceAttackKeyCode
                                                 voiceattackBinding.KeyID, //VoiceAttackKeyId
                                                 voiceattackBinding.ModifierKeyGameValue, //VoiceAttackModifierKeyValue
                                                 voiceattackBinding.ModifierKeyEnumerationCode, //VoiceAttackModifierKeyCode
                                                 voiceattackBinding.ModifierKeyID, //VoiceAttackModifierKeyId
                                                 ////--------------------------------------------------------------------------                                                 
                                                 elitedangerousBinding.KeyPriority, //EliteDangerousDevicePriority
                                                 elitedangerousBinding.KeyGameValue, //EliteDangerousKeyValue
                                                 elitedangerousBinding.KeyEnumerationCode, //EliteDangerousKeyCode
                                                 elitedangerousBinding.KeyID, //EliteDangerousKeyId
                                                 elitedangerousBinding.ModifierKeyGameValue, //EliteDangerousModifierKeyValue
                                                 elitedangerousBinding.ModifierKeyEnumerationCode, //EliteDangerousModifierKeyCode
                                                 elitedangerousBinding.ModifierKeyID, //EliteDangerousModifierKeyId
                                                 ////--------------------------------------------------------------------------
                                                 voiceattackBinding.Internal, //VoiceAttackInternal
                                                 voiceattackBinding.FilePath, //VoiceAttackProfile
                                                 elitedangerousBinding.Internal, //EliteDangerousInternal
                                                 elitedangerousBinding.FilePath //EliteDangerousFilePath
                                                 ////--------------------------------------------------------------------------
                                                }, 
                                                false);
                }

                // If not defined in Elite Dangerous binding file ..
                if (!commandDefinedInEliteDangerousBindsFile)
                {
                    // Append to DataTable
                    remapRequired = Enums.KeyUpdateRequired.YES_va_to_ed.ToString();
                    rationale = string.Format("ED[{0}] not bound to a key", voiceattackBinding.EliteDangerousAction);
                    consolidatedaction.LoadDataRow(new object[] 
                                                {
                                                 ////--------------------------------------------------------------------------
                                                 voiceattackBinding.KeyEnumeration, //KeyEnumeration
                                                 ////--------------------------------------------------------------------------
                                                 remapRequired, //ReMapRequired
                                                 rationale, //Rationale
                                                 ////--------------------------------------------------------------------------
                                                 voiceattackBinding.Action, //VoiceAttackAction
                                                 voiceattackBinding.EliteDangerousAction, //EliteDangerousAction
                                                 ////--------------------------------------------------------------------------
                                                 voiceattackBinding.KeyValue, //VoiceAttackKeyValue
                                                 voiceattackBinding.KeyCode, //VoiceAttackKeyCode
                                                 voiceattackBinding.KeyID, //VoiceAttackKeyId
                                                 voiceattackBinding.ModifierKeyGameValue, //VoiceAttackModifierKeyValue
                                                 voiceattackBinding.ModifierKeyEnumerationCode, //VoiceAttackModifierKeyCode
                                                 voiceattackBinding.ModifierKeyID, //VoiceAttackModifierKeyId
                                                 ////--------------------------------------------------------------------------
                                                 StatusCode.NotApplicable, //EliteDangerousDevicePriority                 
                                                 StatusCode.NotApplicable, //EliteDangerousKeyValue
                                                 StatusCode.NotApplicableInt.ToString(), //EliteDangerousKeyCode
                                                 StatusCode.NotApplicable, //EliteDangerousKeyId
                                                 StatusCode.NotApplicable, //EliteDangerousModifierKeyValue
                                                 StatusCode.NotApplicableInt.ToString(), //EliteDangerousModifierKeyCode
                                                 StatusCode.NotApplicable, //EliteDangerousModifierKeyId
                                                 ////--------------------------------------------------------------------------                                               
                                                 voiceattackBinding.Internal, //VoiceAttackInternal
                                                 voiceattackBinding.FilePath, //VoiceAttackProfile
                                                 StatusCode.NotApplicable, //EliteDangerousInternal
                                                 StatusCode.NotApplicable //EliteDangerousFilePath
                                                 ////--------------------------------------------------------------------------
                                                },
                                                false);
                }
            }

            return consolidatedaction;
        }
    }
}