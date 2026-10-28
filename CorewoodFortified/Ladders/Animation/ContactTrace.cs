using System;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;
using UnityEngine;

namespace CorewoodFortified.LadderAnimation;

internal static class ContactTrace
{
	private const int MaximumRows = 40000;

	private static StreamWriter? _writer;

	private static int _rows;

	private static bool _failed;

	private static readonly string[] Names = new string[4] { "LH", "RH", "LF", "RF" };

	internal static void Row(in ClimbPresentationFrame frame, int limb, OriginalClimbPresenter.ContactRecord record, Vector3 patch, float error, float gap, float kneeOut = 0f, float kneeToward = 0f)
	{
		if (_failed || _rows >= 40000)
		{
			return;
		}
		try
		{
			if (_writer == null)
			{
				_writer = new StreamWriter(Path.Combine(Paths.BepInExRootPath, "ladder_contact_trace.csv"), append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
				_writer.WriteLine("time,motion,face,feetY,limb,planted,skipped,native,requested,effective,reach,targetX,targetY,targetZ,patchX,patchY,patchZ,error,gap,rootX,rootY,rootZ,chain,progress,kneeOut,kneeToward,descending");
			}
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			_writer.WriteLine(string.Join(",", Time.time.ToString("0.0000", invariantCulture), frame.Motion.ToString(), frame.Face.ToString(), frame.Feet.Y.ToString("0.0000", invariantCulture), Names[limb], record.Planted ? "1" : "0", record.Skipped ? "1" : "0", record.Native ? "1" : "0", record.Requested.ToString("0.000", invariantCulture), record.Effective.ToString("0.000", invariantCulture), record.Ratio.ToString("0.000", invariantCulture), record.Target.x.ToString("0.0000", invariantCulture), record.Target.y.ToString("0.0000", invariantCulture), record.Target.z.ToString("0.0000", invariantCulture), patch.x.ToString("0.0000", invariantCulture), patch.y.ToString("0.0000", invariantCulture), patch.z.ToString("0.0000", invariantCulture), error.ToString("0.0000", invariantCulture), gap.ToString("0.0000", invariantCulture), record.Root.x.ToString("0.0000", invariantCulture), record.Root.y.ToString("0.0000", invariantCulture), record.Root.z.ToString("0.0000", invariantCulture), record.Chain.ToString("0.000", invariantCulture), record.Progress.ToString("0.000", invariantCulture), kneeOut.ToString("0.0000", invariantCulture), kneeToward.ToString("0.0000", invariantCulture), record.Descending ? "1" : "0"));
			if (++_rows % 200 == 0)
			{
				_writer.Flush();
			}
		}
		catch (Exception)
		{
			_failed = true;
			try
			{
				_writer?.Dispose();
			}
			catch
			{
			}
			_writer = null;
		}
	}

	internal static void Flush()
	{
		try
		{
			_writer?.Flush();
		}
		catch
		{
		}
	}
}
