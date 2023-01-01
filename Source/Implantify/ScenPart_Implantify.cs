using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static System.Net.Mime.MediaTypeNames;

namespace Implantify
{
	[StaticConstructorOnStartup]
	public class ScenPart_Implantify : ScenPart_PawnModifier
	{
		private HediffDef hediffDef;
		private BodyPartRecord bodyPart;


		public HediffDef HediffDef
		{
			get => this.hediffDef;
			set => this.hediffDef = value;
		}
		public BodyPartRecord BodyPart
		{
			get => this.bodyPart;
			set => this.bodyPart = value;
		}


		/// <summary>
		/// Register new ScenPart
		/// </summary>
		static ScenPart_Implantify()
		{
			ScenPartDef scenPart = new ScenPartDef
			{
				defName = "AdvancedStartingHediff",
				label = "Force advaced health condition",
				scenPartClass = typeof(ScenPart_Implantify),
				category = ScenPartCategory.StartingImportant,
				selectionWeight = 1.0f,
				summaryPriority = 10
			};
			scenPart.ResolveReferences();
			scenPart.PostLoad();
			DefDatabase<ScenPartDef>.Add(scenPart);
		}


		/// <summary>
		/// Generate random hediff on scenPart creation
		/// </summary>
		public override void Randomize()
		{
			base.Randomize();
			HediffDef = PossibleHediffs().RandomElement();
			if(NeedBodyPart(HediffDef))
				BodyPart = PossibleParts(HediffDef).RandomElement();
			chance = 1f;
			context = PawnGenerationContext.PlayerStarter;
		}



		/// <summary>
		/// It seems like a bad idea to merge these hediffs
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool TryMerge(ScenPart other) => false;



		/// <summary>
		/// Generate scenpart widget
		/// </summary>
		/// <param name="listing"></param>
		public override void DoEditInterface(Listing_ScenEdit listing)
		{
			// Based on decompiled ScenPart_ThingCount
			Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 4f);
			Rect rect = new Rect(scenPartRect.x, scenPartRect.y, scenPartRect.width, scenPartRect.height / 4f);
			Rect rect2 = new Rect(scenPartRect.x, scenPartRect.y + scenPartRect.height / 4f, scenPartRect.width, scenPartRect.height / 4f);
			Rect rect3 = new Rect(scenPartRect.x, scenPartRect.y + scenPartRect.height / 2f, scenPartRect.width, scenPartRect.height / 2f);
			// 

			// Hediff selection button
			if (Widgets.ButtonText(rect, HediffDef?.LabelCap ?? new TaggedString("No Hediff")))
			{
				FloatMenuUtility.MakeMenu(PossibleHediffs(), (HediffDef hd) => hd.LabelCap, (HediffDef hd) => delegate
				{
					HediffDef = hd;
					BodyPart = PossibleParts(HediffDef).RandomElement();
				});
			}

			// Body part selection button
			if (HediffDef != null)
			{
				if (NeedBodyPart(HediffDef))
				{
					var possibleParts = PossibleParts(HediffDef);
					if (!possibleParts.NullOrEmpty() && !(possibleParts.First() is null))
					{
						if (BodyPart is null)
						{
							BodyPart = possibleParts.RandomElement();
						}
						Widgets.Label(rect2.LeftPart(0.3333f).Rounded(), "body part");
						if (Widgets.ButtonText(rect2.RightPart(0.6666f).Rounded(), BodyPart.LabelCap))
						{
							FloatMenuUtility.MakeMenu(possibleParts, (BodyPartRecord hd) => hd?.LabelCap ?? "Whole body", (BodyPartRecord hd) => delegate
							{
								BodyPart = hd;
							});
						}
					}
					else
					{
						BodyPart = null;
					}
				}
			}

			// Vanilla settings (chance, affected pawns)
			DoPawnModifierEditInterface(rect3.BottomPartPixels(ScenPart.RowHeight * 2f));
		}



		/// <summary>
		/// Data, used for save/load this scenPart
		/// </summary>
		public override void ExposeData()
		{
			base.ExposeData();
			try
			{
				Scribe_Defs.Look(ref hediffDef, label: "implantifyHediffDef");
				Scribe_BodyParts.Look(ref bodyPart, label: "implantifyBodyPart");
			}
			catch { hediffDef = null; bodyPart = null; }
		}




		public override string Summary(Scenario scen)
		{
			return "ScenPart_PawnsHaveHediff".Translate(context.ToStringHuman(), chance.ToStringPercent(), HediffDef?.label ?? "No Hediff").CapitalizeFirst();
		}



		/// <summary>
		/// Executed every time a pawn is created (for all pawns except starting)
		/// </summary>
		/// <param name="p">Created pawn</param>
		protected override void ModifyNewPawn(Pawn p)
		{
			AddHediff(p);
		}



		/// <summary>
		/// Executed after generating a map (for starting pawns)
		/// </summary>
		/// <param name="p">Starting or optional pawn</param>
		protected override void ModifyHideOffMapStartingPawnPostMapGenerate(Pawn p)
		{
			AddHediff(p);
		}



		/// <summary>
		/// Get list of possible body parts for selected hediff
		/// </summary>
		/// <param name="hediff">selected hediff</param>
		/// <returns></returns>
		private List<BodyPartRecord> PossibleParts(HediffDef hediff)
		{
			// I don't know how to find out the PawnKindDefs that will be used after the scenario launch (like Start With Humanlikes from HAL)
			// so I used vanilla colonist for the base body model, so hediffs that require custom body parts
			// cannot be applied by this mod.
			// Maybe...
			// GetListOfAllowedBodyPartRecords defined in the HealthUtils class
			var list = PawnKindDefOf.Colonist.GetListOfAllowedBodyPartRecords(HediffDef);

			return list;
		}



		/// <summary>
		/// Check if this hediff need to specify the body part to apply.
		/// Decompiled code from VOID's Character Editor
		/// </summary>
		/// <param name="hediff"></param>
		/// <returns></returns>
		private static bool NeedBodyPart(HediffDef hediff)
		{
			if (hediff == null)
			{
				return false;
			}
			if (hediff.defName.Contains("_Force") && hediff.hediffClass == typeof(HediffWithComps))
			{
				return false;
			}
			if (hediff.hediffClass == typeof(Hediff_AddedPart) || hediff.hediffClass == typeof(Hediff_Injury) || hediff.hediffClass == typeof(HediffWithComps) || hediff.hediffClass == typeof(Hediff_MissingPart) || hediff.hediffClass == typeof(Hediff_Implant))
			{
				Log.Message($"{hediff.LabelCap} don't need bodypart");
				return true;
			}
			return false;
		}



		/// <summary>
		/// Get hediffs list for "Force advanced health conditions"
		/// </summary>
		/// <returns></returns>
		private IEnumerable<HediffDef> PossibleHediffs()
		{
			// Getting all hediffs that can't be used in vanilla ScenPart_ForcedHediff
			var hediffs = DefDatabase<HediffDef>.AllDefsListForReading.Where(h => (!h.scenarioCanAdd));

			// Return list ordered by translated label
			return hediffs.OrderBy(h => h.LabelCap.RawText);
		}



		/// <summary>
		/// Apply selected hediff to pawn
		/// </summary>
		/// <param name="p"></param>
		private void AddHediff(Pawn p)
		{
			// Seems like it can work without try-catch until first exception, but after that all scenParts of this type will be ignored
			// Game won't crash, but may produce some weird results
			try
			{
				/*
				 * Look at this kludge.
				 * Different races can have different BodyPartDefs with the same defNames.
				 * So there is attempt to find these body parts by the name of slected 'human' body part
				 */
				var newBodyPart = BodyPart;
				// GetListOfBodyPartRecordsByName defined in the HealthUtils class
				List<BodyPartRecord> foundBodyParts = new List<BodyPartRecord>();
				try
				{
					foundBodyParts = p.kindDef.GetListOfBodyPartRecordsByName(bodyPart.def.defName, HediffDef);
					foreach (var foundBodyPart in foundBodyParts)
					{
						if (foundBodyPart.LabelCap.Equals(BodyPart.LabelCap))
						{
							newBodyPart = foundBodyPart;
							break;
						}
					}
				}
				catch(Exception e)
				{
					Log.Error("Implantify.AddHediff: "+e.Message);
				}
				
				Hediff hediff = HediffMaker.MakeHediff(HediffDef, p, newBodyPart);
				p.health.AddHediff(hediff, newBodyPart);

				// IDK what this code does. Is it some kind of event system?
				p.health.Notify_HediffChanged(hediff);
			}
			catch (Exception e)
			{
				Log.Error(e.Message);
			}
			p.needs?.AddOrRemoveNeedsAsAppropriate();
			p.health.summaryHealth.Notify_HealthChanged();
		}
	}
}
