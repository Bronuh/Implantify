using System;
using System.Collections.Generic;
using System.Linq;
using Verse;


namespace Implantify;

/// <summary>
/// Partially decompiled code from VOID's Character Editor
/// https://steamcommunity.com/sharedfiles/filedetails/?id=1874644848&searchtext=Character+Editor
/// </summary>
static class HealthUtils
{
	public static List<BodyPartRecord> GetListOfBodyPartRecordsByName(this PawnKindDef p, string defName, HediffDef h, bool all = false)
	{
		List<BodyPartRecord> list = new List<BodyPartRecord>();
		if (p != null && h != null)
		{
			if (defName is null || defName == "WholeBody")
			{
				list.Add(null);
			}
			else
			{
				foreach (BodyPartRecord allPart in p.RaceProps.body.AllParts)
				{
					if (all)
					{
						if (!list.Contains(allPart))
						{
							list.Add(allPart);
						}
					}
					else if (!h.hediffGivers.NullOrEmpty())
					{
						foreach (HediffGiver hediffGiver in h.hediffGivers)
						{
							if (hediffGiver.partsToAffect.NullOrEmpty())
							{
								if (allPart.def.defName == defName)
								{
									if (!list.Contains(allPart))
									{
										list.Add(allPart);
									}
									break;
								}
							}
							else if (hediffGiver.partsToAffect.Contains(allPart.def))
							{
								if (!list.Contains(allPart))
								{
									list.Add(allPart);
								}
								break;
							}
						}
					}
					else if (allPart.def.defName == defName && !list.Contains(allPart))
					{
						list.Add(allPart);
					}
				}
			}
		}
		return list;
	}

	public static List<BodyPartRecord> GetListOfAllowedBodyPartRecords(this PawnKindDef p, HediffDef h)
	{
		if (p == null || h == null)
		{
			return null;
		}
		List<BodyPartRecord> l = new List<BodyPartRecord>();
		if (l.NullOrEmpty() && !h.tags.NullOrEmpty())
		{
			List<BodyPartRecord> list = new List<BodyPartRecord>();
			foreach (string tag in h.tags)
			{
				list = p.GetListOfBodyPartRecordsByName(tag, h, tag == "All");
				if (!list.NullOrEmpty())
				{
					l.AddRange(list);
				}
			}
		}
		if (l.NullOrEmpty())
		{
			foreach (string key in AllBodyPartChecks.Keys)
			{
				if (AllBodyPartChecks[key](h))
				{
					l = p.GetListOfBodyPartRecordsByName(key, h, key == "All");
					break;
				}
			}
		}

		/// Attempt to scan surgery recipes for body parts
		if (l.NullOrEmpty())
		{
			// Get all recipes containing target hediff
			var recipes = DefDatabase<RecipeDef>.AllDefs.Where(def => def.addsHediff == h);

			foreach(var recipeDef in recipes)
			{
				IEnumerable<BodyPartRecord> records;

				// converting recipe body parts to body part records
				foreach (var bodyPart in recipeDef.appliedOnFixedBodyParts)
				{
					records = p.RaceProps.body.AllParts.Where(part => part.def == bodyPart);

					foreach (var bodyPartRecord in records)
					{
						if (!l.Contains(bodyPartRecord))
						{
							l.Add(bodyPartRecord);
						}
					}
				}
			}
		}

		if (l.NullOrEmpty())
		{
			l = p.GetListOfBodyPartRecordsByName(null, h, all: true);
			bool flag = h.modContentPack == null || !h.modContentPack.IsCoreMod;
			if (h.injuryProps == null && !h.HasComp(typeof(HediffComp_TendDuration)) && (h.IsHediffWithComps() || flag))
			{
				l.Insert(0, null);
			}
		}
			

		return l;
	}


	public static bool IsHediffWithComps(this HediffDef h)
	{
		return h != null && h.hediffClass != null && (h.hediffClass == typeof(HediffWithComps) || h.hediffClass.BaseType == typeof(HediffWithComps));
	}

	#region Checks
	public static Dictionary<string, Func<HediffDef, bool>> AllBodyPartChecks
	{
		get
		{
			Dictionary<string, Func<HediffDef, bool>> dictionary = new Dictionary<string, Func<HediffDef, bool>>();
			dictionary.Add("All", IsForAllParts);
			dictionary.Add("AllImplants", IsForAllParts);
			dictionary.Add("AllAddictions", IsForAllParts);
			dictionary.Add("AllDiseases", IsForAllParts);
			dictionary.Add("AllInjuries", IsForAllParts);
			dictionary.Add("AllTime", IsForAllParts);
			dictionary.Add("Arm", IsForArm);
			dictionary.Add("Brain", IsForBrain);
			dictionary.Add("Clavicle", IsForClavicle);
			dictionary.Add("Ear", IsForEar);
			dictionary.Add("Eye", IsForEye);
			dictionary.Add("Femur", IsForFemur);
			dictionary.Add("Finger", IsForFinger);
			dictionary.Add("Foot", IsForFoot);
			dictionary.Add("Hand", IsForHand);
			dictionary.Add("Head", IsForHead);
			dictionary.Add("Heart", IsForHeart);
			dictionary.Add("Humerus", IsForHumerus);
			dictionary.Add("Jaw", IsForJaw);
			dictionary.Add("Kidney", IsForKidney);
			dictionary.Add("Leg", IsForLeg);
			dictionary.Add("Liver", IsForLiver);
			dictionary.Add("Lung", IsForLung);
			dictionary.Add("Nose", IsForNose);
			dictionary.Add("Pelvis", IsForPelvis);
			dictionary.Add("Radius", IsForRadius);
			dictionary.Add("Shoulder", IsForShoulder);
			dictionary.Add("Skull", IsForSkull);
			dictionary.Add("Spine", IsForSpine);
			dictionary.Add("Sternum", IsForSternum);
			dictionary.Add("Stomach", IsForStomach);
			dictionary.Add("Tibia", IsForTibia);
			dictionary.Add("Toe", IsForToe);
			dictionary.Add("Torso", IsForTorso);
			dictionary.Add("UtilitySlot", IsForUtilitySlot);
			dictionary.Add("WholeBody", IsForWholeBody);
			return dictionary;
		}
	}
	public static bool IsFor(HediffDef h, string bodyparttype)
	{
		return AllBodyPartChecks[bodyparttype](h);
	}

	public static bool IsForAllParts(this HediffDef h)
	{
		return h.hediffClass == typeof(Hediff_MissingPart) || h.defName == "ChemicalDamageModerate" || h.defName == "ChemicalDamageSevere" || h.defName == "MuscleParasites" || h.defName == "FibrousMechanites" || h.defName == "SensoryMechanites" || h.defName == "Carcinoma" || h.defName == "WoundInfection";
	}

	public static bool IsForArm(this HediffDef h)
	{
		return h.defName.Contains("Arm") || h.defName == "PowerClaw" || h.defName == "ElbowBlade";
	}

	public static bool IsForBrain(this HediffDef h)
	{
		return h.defName.Contains("Brain") || (h.defName.StartsWith("Psychic") && h.defName != "PsychicEntropy") || (h.HasComp(typeof(HediffComp_Disappears)) && h.defName.Contains("_Psychic")) || h.defName.StartsWith("Circadian") || h.defName == "Dementia" || h.defName == "Alzheimers" || h.defName == "ResurrectionPsychosis" || h.defName == "TraumaSavant" || h.defName == "Joywire" || h.defName == "Painstopper" || h.defName == "Neurocalculator" || h.defName == "LearningAssistant" || h.defName == "Mindscrew" || h.defName == "Joyfuzz" || h.defName == "Abasia" || h.defName == "NoPain" || h.defName == "HungerMaker" || h.defName == "SpeedBoost" || h.defName.EndsWith("Command") || h.HasComp(typeof(HediffComp_EntropyLink)) || h.HasComp(typeof(HediffComp_Link));
	}

	public static bool IsForClavicle(this HediffDef h)
	{
		return h.defName.Contains("Clavicle");
	}

	public static bool IsForEar(this HediffDef h)
	{
		return h.defName.Contains("Ear") || h.defName.Contains("Hearing") || h.defName.Contains("Cochlear") || h.defName == "HearingLoss";
	}

	public static bool IsForEye(this HediffDef h)
	{
		return h.defName.Contains("Eye") || h.defName == "Cataract" || h.defName == "Blindness";
	}

	public static bool IsForFemur(this HediffDef h)
	{
		return h.defName.Contains("Femur");
	}

	public static bool IsForFinger(this HediffDef h)
	{
		return h.defName.Contains("Finger") || h.defName == "VenomTalon";
	}

	public static bool IsForFoot(this HediffDef h)
	{
		return h.defName.Contains("Foot");
	}

	public static bool IsForHand(this HediffDef h)
	{
		return h.defName.Contains("Hand");
	}

	public static bool IsForHead(this HediffDef h)
	{
		return h.defName.Contains("Head") || h.defName == "Hangover" || h.defName == "TortureCrown" || h.defName == "Blindfold";
	}

	public static bool IsForHeart(this HediffDef h)
	{
		return h.defName.Contains("Heart");
	}

	public static bool IsForHumerus(this HediffDef h)
	{
		return h.defName.Contains("Humerus");
	}

	public static bool IsForJaw(this HediffDef h)
	{
		return h.defName.Contains("Jaw") || h.defName.StartsWith("Denture") || h.defName.Contains("Fangs");
	}

	public static bool IsForKidney(this HediffDef h)
	{
		return h.defName.Contains("Kidney") || h.defName == "Immunoenhancer";
	}

	public static bool IsForLeg(this HediffDef h)
	{
		return h.defName.Contains("Leg") || h.defName == "KneeSpike";
	}

	public static bool IsForLiver(this HediffDef h)
	{
		return h.defName.Contains("Liver") || h.defName == "Cirrhosis" || h.defName == "AlcoholTolerance";
	}

	public static bool IsForLung(this HediffDef h)
	{
		return h.defName.Contains("Lung") || h.defName == "Asthma";
	}

	public static bool IsForNose(this HediffDef h)
	{
		return h.defName.Contains("Nose") || h.defName.Contains("Smelling") || h.defName == "GastroAnalyzer";
	}

	public static bool IsForPelvis(this HediffDef h)
	{
		return h.defName.Contains("Pelvis");
	}

	public static bool IsForRadius(this HediffDef h)
	{
		return h.defName.Contains("Radius");
	}

	public static bool IsForShoulder(this HediffDef h)
	{
		return h.defName.Contains("Shoulder");
	}

	public static bool IsForSkull(this HediffDef h)
	{
		return h.defName.Contains("Skull");
	}

	public static bool IsForSpine(this HediffDef h)
	{
		return h.defName.Contains("Spine") || h.defName == "BadBack";
	}

	public static bool IsForSternum(this HediffDef h)
	{
		return h.defName.Contains("Sternum");
	}

	public static bool IsForStomach(this HediffDef h)
	{
		return h.defName.Contains("Stomach") || h.defName == "GutWorms";
	}

	public static bool IsForTibia(this HediffDef h)
	{
		return h.defName.Contains("Tibia");
	}

	public static bool IsForToe(this HediffDef h)
	{
		return h.defName.Contains("Toe");
	}

	public static bool IsForTorso(this HediffDef h)
	{
		return h.defName.Contains("Torso") || h.defName.EndsWith("skinGland") || h.defName == "Coagulator" || h.defName == "HealingEnhancer" || h.defName == "AestheticShaper" || h.defName == "LoveEnhancer";
	}

	public static bool IsForUtilitySlot(this HediffDef h)
	{
		return h.defName.Contains("Utility");
	}

	public static bool IsForWholeBody(this HediffDef h)
	{
		return h.IsAddiction || h.defName.ToLower().Contains("tolerance") || h.defName.ToLower().Contains("regen") || h.defName.ToLower().Contains("lactation") || h.defName.ToLower().Contains("lactating") || h.defName.EndsWith("High") || h.defName == "PsychicEntropy" || h.defName == "PsychicShock" || h.defName == "NeuralHealRecoveryGain" || h.defName == "NeuralSupercharge" || h.defName == "WorkDrive" || h.defName == "ImmunityDrive" || h.defName == "WorkFocus" || h.defName == "PreachHealth" || h.defName == "BerserkTrance" || h.defName == "CatatonicBreakdown" || h.defName == "BloodLoss" || h.defName.EndsWith("Flu") || h.defName.EndsWith("Plague") || h.defName == "Malaria" || h.defName == "SleepingSickness" || h.defName == "Anesthetic" || h.defName == "Frail" || h.defName == "CryptosleepSickness" || h.defName == "FoodPoisoning" || h.defName == "ToxicBuildup" || h.defName == "Pregnant" || h.defName == "DrugOverdose" || h.defName == "ResurrectionSickness" || h.defName == "Malnutrition";
	}

	#endregion
}