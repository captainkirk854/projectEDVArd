﻿namespace Binding
{
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Helper;

    /// <summary>
    /// Parse HCSVoicePacks Voice Attack Profile file
    /// </summary>
    public class KeyReaderVoiceAttack : KeyReader, IKeyReader
    {
        // Initialise ..
        private const string XMLRoot = "Profile";
        private const string XMLName = "Name";
        private const string XMLCommand = "Command";
        private const string XMLCommandId = "Id";
        private const string XMLCommandString = "CommandString";
        private const string XMLCategory = "Category";
        private const string XMLActionSequence = "ActionSequence";
        private const string XMLCommandAction = "CommandAction";
        private const string XMLActionType = "ActionType";
        private const string XMLActionId = "Id";
        private const string XMLKeyCodes = "KeyCodes";
        private const string XMLContext = "Context";
        private const string XMLunsignedShort = "unsignedShort";
        private const string KeybindingCategoryHCSVoicePack = "Keybindings";
        
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyReaderVoiceAttack" /> class.
        /// Base class constructor loads config.file as XDocument (this.xCfg)
        /// </summary>
        /// <param name="cfgFilePath"></param>
        public KeyReaderVoiceAttack(string cfgFilePath) : base(cfgFilePath)
        {
        }
   
        /// <summary>
        /// Load Voice Attack Commands mapped to Elite Dangerous Key-Bindable Actions into DataTable
        /// </summary>
        /// <returns></returns>
        public DataTable GetBindableCommands()
        {
            // Read bindings and tabulate ..
            DataTable primary = this.GetBindableActions(ref this.xCfg);

            // Add column ..
            primary.AddDefaultColumn(Enums.Column.Internal.ToString(), this.GetInternalReference(ref this.xCfg));

            // Add column ..
            primary.AddDefaultColumn(Enums.Column.FilePath.ToString(), this.cfgFilePath);

            // return Datatable ..
            return primary;
        }

        /// <summary>
        /// Load Voice Attack Key Bindings into DataTable
        /// </summary>
        /// <returns></returns>
        public DataTable GetBoundCommands()
        {
            // Read bindings and tabulate ..
            DataTable primary = this.GetKeyBindings(ref this.xCfg);

            // Add column ..
            primary.AddDefaultColumn(Enums.Column.Internal.ToString(), this.GetInternalReference(ref this.xCfg));

            // Add column ..
            primary.AddDefaultColumn(Enums.Column.FilePath.ToString(), this.cfgFilePath);

            // return Datatable ..
            return primary;
        }

        /// <summary>
        /// Get other CommandStrings associated with CommandString
        /// </summary>
        /// <param name="consolidatedBoundCommands"></param>
        /// <returns></returns>
        public DataTable GetAssociatedCommandStrings(DataTable consolidatedBoundCommands)
        {
            // Initialise ..
            DataTable associatedCommands = TableShape.AssociatedCommands();
            string prevCommandString = string.Empty;

            // Find associated CommandStrings using CommandString ActionId ...
            foreach (DataRow consolidatedBoundCommandRow in consolidatedBoundCommands.Select().OrderBy(orderingColumn => orderingColumn[Enums.Column.VoiceAttackAction.ToString()]))
            {
                // Get required field information ..
                string voiceattackCommandString = consolidatedBoundCommandRow[Enums.Column.VoiceAttackAction.ToString()].ToString();
                string voiceattackActionId = consolidatedBoundCommandRow[Enums.Column.VoiceAttackKeyId.ToString()].ToString();
                string elitedangerousAction = consolidatedBoundCommandRow[Enums.Column.EliteDangerousAction.ToString()].ToString();
                string bindingSyncStatus = consolidatedBoundCommandRow[Enums.Column.KeyUpdateRequired.ToString()].ToString() == Enums.KeyUpdateRequired.NO.ToString() ? "synchronised" : "*attention required*";
                string voiceattackFile = Path.GetFileName(consolidatedBoundCommandRow[Enums.Column.VoiceAttackProfile.ToString()].ToString());
                string eliteDangerousFile = Path.GetFileName(consolidatedBoundCommandRow[Enums.Column.EliteDangerousBinds.ToString()].ToString());

                // Ignore duplicate CommandStrings from those with multiple Action Ids ..
                if (voiceattackCommandString != prevCommandString)
                {
                    var associatedCommandStrings = this.GetCommandStringsFromCommandContext(ref this.xCfg, this.GetCommandIdFromActionId(ref this.xCfg, voiceattackActionId));
                    
                    string prevAssociatedCommandString = string.Empty;
                    foreach (var associatedCommandString in associatedCommandStrings)
                    {
                        // Ignore any duplicate Associated CommandStrings ..
                        if (associatedCommandString != prevAssociatedCommandString)
                        {
                            associatedCommands.LoadDataRow(new object[] 
                                                           {
                                                                voiceattackCommandString,
                                                                elitedangerousAction,
                                                                associatedCommandString,
                                                                bindingSyncStatus,
                                                                voiceattackFile,
                                                                eliteDangerousFile
                                                           },
                                                           false);
                        }

                        prevAssociatedCommandString = associatedCommandString;
                    }
                }

                prevCommandString = voiceattackCommandString;
            }

            return associatedCommands;
        }

        /// <summary>
        /// Parse Voice Attack Config File to summarise all possible Elite Dangerous specific Commands with key-bindable actions as defined by HCSVoicePacks
        /// </summary>
        /// <param name="xdoc"></param>
        /// <returns></returns>
        private DataTable GetBindableActions(ref XDocument xdoc)
        {
            // Datatable to hold tabulated XML contents ..
            DataTable bindableactions = TableShape.BindableActions();

            // traverse config XML, find all valuated <unsignedShort> nodes, work from inside out to gather pertinent Element data and arrange in row(s) of anonymous types ..
            var xmlExtracts = from item in xdoc.Descendants(XMLunsignedShort)
                              where
                                    item.Parent.Parent.Parent.Parent.Element(XMLCategory).Value == KeybindingCategoryHCSVoicePack &&
                                    (item.Parent.Parent.Parent.Parent.Element(XMLActionSequence).Element(XMLCommandAction).Element(XMLActionType).Value == Enums.Interaction.PressKey.ToString() ||
                                     item.Parent.Parent.Parent.Parent.Element(XMLActionSequence).Element(XMLCommandAction).Element(XMLActionType).Value == Enums.Interaction.ExecuteCommand.ToString()) &&
                                    item.SafeElementValue() != string.Empty
                              select
                                 new
                                 {
                                     Commandstring = item.Parent.Parent.Parent.Parent.Element(XMLCommandString).SafeElementValue()
                                 };

            // insert anonymous type row data (with some additional values) into DataTable (.Distinct() required as some Commands have multiple (modifier) key codes)
            foreach (var xmlExtract in xmlExtracts.Distinct()) 
            {
                bindableactions.LoadDataRow(new object[] 
                                                {
                                                    Enums.Game.VoiceAttack.ToString(), //Context
                                                    xmlExtract.Commandstring, //BindingAction
                                                    StatusCode.NotApplicable, // Device priority
                                                    Enums.Interaction.Keyboard.ToString() // Device binding is applied to
                                                }, 
                                                false);
            }

            // return Datatable ..
            return bindableactions;
        }

        /// <summary>
        /// Parse Voice Attack Config File to get Command Id
        /// </summary>
        /// <remarks>
        ///   Format: XML
        ///             o <Profile/>
        ///               |_ <Commands/>
        ///                  |_ <Command/>
        ///                      |_<Id/> <---------------------------------¬
        ///                      !_<CommandString/> = ((<action name/>))   |
        ///                      |_<ActionSequence/>                       |
        ///                        !_[some] <CommandAction/>               |
        ///                                 !_<Id/> ------------------------
        ///                                 |_<ActionType/> = PressKey
        ///                                 |_<KeyCodes/>
        ///                                   (|_<unsignedShort/> = when modifier present)
        ///                                    |_<unsignedShort/>
        ///                      !_<Category/> = Keybindings
        /// </remarks>
        /// <param name="xdoc"></param>
        /// <param name="actionId"></param>
        /// <returns></returns>
        private string GetCommandIdFromActionId(ref XDocument xdoc, string actionId)
        {
            // traverse config XML to <unsignedShort> nodes, return first (and only) ancestral Command Id where Action Id is ancestor of <unsignedShort> ..
            var xmlExtracts = from item in xdoc.Descendants(XMLunsignedShort)
                              where
                                    item.Parent.Parent.Parent.Parent.Element(XMLCategory).Value == KeybindingCategoryHCSVoicePack &&
                                    item.Parent.Parent.Element(XMLActionId).SafeElementValue() == actionId
                              select
                                     item.Parent.Parent.Parent.Parent.Element(XMLCommandId).SafeElementValue();

            return xmlExtracts.FirstOrDefault();       
        }

        /// <summary>
        /// Parse Voice Attack Config File to get Command String
        /// </summary>
        /// <remarks>
        ///   Format: XML
        ///             o <Profile/>
        ///               |_ <Commands/>
        ///                  |_ <Command/>
        ///                      |_<Id/>
        ///                      !_<CommandString/> = Spoken Command  <---------¬           
        ///                      |_<ActionSequence/>                            |
        ///                        !_[some] <CommandAction/>                    |
        ///                                 |_<KeyCodes/>                       |
        ///                                 |_<Context/> ------------------------
        ///                      !_<Description/> = Command Description
        ///                      !_<Category/> != Keybindings
        /// </remarks>
        /// <param name="xdoc"></param>
        /// <param name="contextId"></param>
        /// <returns></returns>
        private System.Collections.Generic.IEnumerable<string> GetCommandStringsFromCommandContext(ref XDocument xdoc, string contextId)
        {
            // traverse config XML, return first (and only) Command Id where Action Id is descendant ..
            var xmlExtracts = from item in xdoc.Descendants(XMLContext)
                              where
                                    item.SafeElementValue() == contextId &&
                                    item.Parent.Parent.Parent.Element(XMLCategory).Value != KeybindingCategoryHCSVoicePack
                              select
                                     item.Parent.Parent.Parent.Element(XMLCommandString).SafeElementValue();

            return xmlExtracts;
        }

        /// <summary>
        /// Parse Voice Attack Config File to get details of all possible Elite Dangerous specific Commands with key-bindable actions as defined by HCSVoicePacks
        /// </summary>
        /// <remarks>
        ///   Format: XML
        ///             o <Profile/>
        ///               |_ <Commands/>
        ///                  |_ <Command/>
        ///                      |_<Id/>
        ///                      !_<CommandString/> = ((<action name/>))
        ///                      |_<ActionSequence/>
        ///                        !_[some] <CommandAction/>
        ///                                 !_<Id/>
        ///                                 |_<ActionType/> = PressKey
        ///                                 |_<KeyCodes/>
        ///                                   (|_<unsignedShort/> = when modifier present)
        ///                                    |_<unsignedShort/>
        ///                      !_<Category/> = Keybindings
        ///                             
        /// Keys Bindings: 
        ///                VA uses actual key codes (as opposed to key value). 
        ///                Actions directly mappable to Elite Dangerous have been defined by HCSVoicePacks using: 
        ///                 o Command.Category = Keybindings
        ///                 o Command.CommandString values which are pre- and post-fixed using '((' and '))'
        ///                   e.g. 
        ///                    ((Shield Cell)) : 222 (= Oem7 Numpad?7)
        ///                    ((Power To Weapons)) : 39  (= Right arrow)
        ///                    ((Select Target Ahead)) : 84 (= T)
        ///                    ((Flight Assist)) : 90 (= Z)
        ///                   
        ///                Note 
        ///                There are other commands that also use key codes which are part of the multi-command suite.
        ///                These are ignored
        /// </remarks>
        /// <param name="xdoc"></param>
        /// <returns></returns>
        private DataTable GetKeyBindings(ref XDocument xdoc)
        {
            // Datatable to hold tabulated XML contents ..
            DataTable keyactionbinder = TableShape.KeyActionBinder();

            // traverse config XML, find all valuated <unsignedShort> nodes, work from inside out to gather pertinent Element data and arrange in row(s) of anonymous types ..
            var xmlExtracts = from item in xdoc.Descendants(XMLunsignedShort)
                              where
                                    item.Parent.Parent.Parent.Parent.Element(XMLCategory).Value == KeybindingCategoryHCSVoicePack &&
                                    (item.Parent.Parent.Parent.Parent.Element(XMLActionSequence).Element(XMLCommandAction).Element(XMLActionType).Value == Enums.Interaction.PressKey.ToString() ||
                                     item.Parent.Parent.Parent.Parent.Element(XMLActionSequence).Element(XMLCommandAction).Element(XMLActionType).Value == Enums.Interaction.ExecuteCommand.ToString()) &&
                                    item.SafeElementValue() != string.Empty
                              select
                                 new // create anonymous type for every XMLunsignedShort matching criteria ..
                                 {
                                     Commandstring = item.Parent.Parent.Parent.Parent.Element(XMLCommandString).SafeElementValue(),
                                     ActionId = item.Parent.Parent.Element(XMLActionId).SafeElementValue(),
                                     KeyCode = item.SafeElementValue()
                                 };

            // insert anonymous type row data (with some additional values) into DataTable ..
            foreach (var xmlExtract in xmlExtracts)
            {
                // Initialise ..
                string modifierKeyEnumerationValue = StatusCode.EmptyString;
                int regularKeyCode = int.Parse(xmlExtract.KeyCode);

                // Check for modifier key already present in VoiceAttack Profile for current Action Id ..
                int modifierKeyCode = this.GetModifierKey(ref xdoc, xmlExtract.ActionId);

                // Ignore if current regular key code is actually a modifier key code ..
                if (regularKeyCode != modifierKeyCode)
                {
                    if (modifierKeyCode >= 0)
                    {
                        // If modifier found, some additional probing of that segment of the XML tree required ..
                        modifierKeyEnumerationValue = KeyMapper.GetValue(modifierKeyCode);
                        regularKeyCode = this.GetRegularKey(ref xdoc, xmlExtract.ActionId);
                    }

                    // Load final values into datatable ..
                    keyactionbinder.LoadDataRow(new object[] 
                                                    {
                                                        Enums.Game.VoiceAttack.ToString(), //Context
                                                        KeyMapper.KeyType.ToString(), //KeyEnumerationType
                                                        xmlExtract.Commandstring, //BindingAction
                                                        StatusCode.NotApplicable, //Priority
                                                        KeyMapper.GetValue(regularKeyCode), //KeyGameValue
                                                        KeyMapper.GetValue(regularKeyCode), //KeyEnumerationValue
                                                        regularKeyCode.ToString(), //KeyEnumerationCode
                                                        xmlExtract.ActionId, //KeyId
                                                        modifierKeyEnumerationValue, //ModifierKeyGameValue
                                                        modifierKeyEnumerationValue, //ModifierKeyEnumerationValue
                                                        modifierKeyCode, //ModifierKeyEnumerationCode
                                                        xmlExtract.ActionId //ModifierKeyId
                                                    },
                                                    false);
                }
            }

            // return Datatable ..
            return keyactionbinder;
        }

        /// <summary>
        /// Parse Voice Attack Config File to find internal reference
        /// </summary>
        /// <remarks>
        ///   Format: XML
        ///             o <Profile/>
        ///               |_ <Name>       
        /// </remarks>
        /// <param name="xdoc"></param>
        /// <returns></returns>
        private string GetInternalReference(ref XDocument xdoc)
        {
            try
            {
                return xdoc.Element(XMLRoot).Element(XMLName).SafeElementValue().Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Get (any) Modifier Key Code associated to Action Id
        /// </summary>
        /// <param name="xdoc"></param>
        /// <param name="actionId"></param>
        /// <returns></returns>
        private int GetModifierKey(ref XDocument xdoc, string actionId)
        {
            // Count number of unsigned short elements (KeyCode) exist per ActionId ...
            var keyCodes = xdoc.Descendants(XMLunsignedShort)
                                    .Where(item => item.Parent.Parent.Parent.Parent.Element(XMLCategory).Value == KeybindingCategoryHCSVoicePack &&
                                                   item.Parent.Parent.Element(XMLActionId).Value == actionId)
                                    .DescendantsAndSelf();

            var countOfKeyCode = keyCodes.Count();

            // Check to see if modifier already exists in VoiceAttack Profile ..
            if (countOfKeyCode > 1)
            {
                // First value is always Modifier Key Code ..
                return int.Parse(keyCodes.FirstOrDefault().Value);
            }
            else
            {
                return StatusCode.EmptyStringInt;
            }
        }

        /// <summary>
        /// Get regular (non-Modifier) Key Code associated to Action Id
        /// </summary>
        /// <param name="xdoc"></param>
        /// <param name="actionId"></param>
        /// <returns></returns>
        private int GetRegularKey(ref XDocument xdoc, string actionId)
        {
            // Count number of unsigned short elements (KeyCode) exist per ActionId ...
            var keyCodes = xdoc.Descendants(XMLunsignedShort)
                                    .Where(item => item.Parent.Parent.Parent.Parent.Element(XMLCategory).Value == KeybindingCategoryHCSVoicePack &&
                                                   item.Parent.Parent.Element(XMLActionId).Value == actionId)
                                    .DescendantsAndSelf();

            var countOfKeyCode = keyCodes.Count();

            // Last value is always Regular Key Code ..
            return int.Parse(keyCodes.LastOrDefault().Value);
        }
    }
}