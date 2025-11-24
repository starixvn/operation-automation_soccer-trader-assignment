using Microsoft.AspNetCore.Mvc;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services;

namespace SoccerLST.TraderAssignment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TraderAssignmentController : ControllerBase
{
    private readonly TraderAssignmentService _assignmentService;
    private readonly ILogger<TraderAssignmentController> _logger;

    public TraderAssignmentController(
        TraderAssignmentService assignmentService,
        ILogger<TraderAssignmentController> logger)
    {
        _assignmentService = assignmentService;
        _logger = logger;
    }

    [HttpPost("mj")]
    public ActionResult<AssignmentResponse> MajorMatchAssignments([FromBody] AssignmentRequest request)
    {
        try
        {
            _logger.LogInformation("Received MJ assignment optimization request with {TraderCount} traders and {MatchCount} matches",
                request.TraderShifts.Count, request.Matches.Count);

            var result = _assignmentService.MajorMatchAssignments(request);

            _logger.LogInformation("MJ assignment optimization completed. Status: {Status}, Assigned: {Assigned}/{Total}",
                result.Status, result.AssignedMatches, result.TotalMatches);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing MJ trader assignments");
            return StatusCode(500, new { error = "An error occurred while optimizing MJ assignments", details = ex.Message });
        }
    }

    [HttpPost("m1")]
    public ActionResult<AssignmentResponse> MinorOneMatchAssignments([FromBody] AssignmentRequest request)
    {
        try
        {
            _logger.LogInformation("Received M1 assignment optimization request with {TraderCount} traders and {MatchCount} matches",
                request.TraderShifts.Count, request.Matches.Count);

            // Validate M1 matches (40K-70K maxBet range, or betType="M1")
            var invalidMatches = request.Matches
                .Where(m => m.BetType != "M1" && (m.MaxBet < 40000 || m.MaxBet >= 70000))
                .ToList();

            if (invalidMatches.Any())
            {
                _logger.LogWarning("Found {Count} matches outside M1 range (40K-70K) without betType='M1'", invalidMatches.Count);
                return BadRequest(new
                {
                    error = "Invalid M1 matches",
                    message = "All matches must have betType='M1' or maxBet between 40,000 and 70,000 for M1 assignment",
                    invalidMatchIds = invalidMatches.Select(m => m.Id).ToList()
                });
            }

            var result = _assignmentService.MinorOneMatchAssignments(request);

            _logger.LogInformation("M1 assignment optimization completed. Status: {Status}, Assigned: {Assigned}/{Total}",
                result.Status, result.AssignedMatches, result.TotalMatches);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing M1 trader assignments");
            return StatusCode(500, new { error = "An error occurred while optimizing M1 assignments", details = ex.Message });
        }
    }

    [HttpPost("m2")]
    public ActionResult<AssignmentResponse> MinorTwoMatchAssignments([FromBody] AssignmentRequest request)
    {
        try
        {
            _logger.LogInformation("Received M2 assignment optimization request with {TraderCount} traders and {MatchCount} matches",
                request.TraderShifts.Count, request.Matches.Count);

            // Validate M2 matches (20K-40K maxBet range, or betType="M2")
            var invalidMatches = request.Matches
                .Where(m => m.BetType != "M2" && (m.MaxBet < 20000 || m.MaxBet >= 40000))
                .ToList();

            if (invalidMatches.Any())
            {
                _logger.LogWarning("Found {Count} matches outside M2 range (20K-40K) without betType='M2'", invalidMatches.Count);
                return BadRequest(new
                {
                    error = "Invalid M2 matches",
                    message = "All matches must have betType='M2' or maxBet between 20,000 and 40,000 for M2 assignment",
                    invalidMatchIds = invalidMatches.Select(m => m.Id).ToList()
                });
            }

            var result = _assignmentService.MinorTwoMatchAssignments(request);

            _logger.LogInformation("M2 assignment optimization completed. Status: {Status}, Assigned: {Assigned}/{Total}",
                result.Status, result.AssignedMatches, result.TotalMatches);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing M2 trader assignments");
            return StatusCode(500, new { error = "An error occurred while optimizing M2 assignments", details = ex.Message });
        }
    }

    [HttpPost("m3")]
    public ActionResult<AssignmentResponse> MinorThreeMatchAssignments([FromBody] AssignmentRequest request)
    {
        try
        {
            _logger.LogInformation("Received M3 assignment optimization request with {TraderCount} traders and {MatchCount} matches",
                request.TraderShifts.Count, request.Matches.Count);


            var result = _assignmentService.MinorThreeMatchAssignments(request);

            _logger.LogInformation("M3 assignment optimization completed. Status: {Status}, Assigned: {Assigned}/{Total}",
                result.Status, result.AssignedMatches, result.TotalMatches);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing M3 trader assignments");
            return StatusCode(500, new { error = "An error occurred while optimizing M3 assignments", details = ex.Message });

        }
    }

    [HttpPost("m4")]
    public ActionResult<AssignmentResponse> MinorFourMatchAssignments([FromBody] AssignmentRequest request)
    {
        try
        {
            _logger.LogInformation("Received M4 assignment optimization request with {TraderCount} traders and {MatchCount} matches",
                request.TraderShifts.Count, request.Matches.Count);


            var result = _assignmentService.MinorFourMatchAssignments(request);

            _logger.LogInformation("M4 assignment optimization completed. Status: {Status}, Assigned: {Assigned}/{Total}",
                result.Status, result.AssignedMatches, result.TotalMatches);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing M4 trader assignments");
            return StatusCode(500, new { error = "An error occurred while optimizing M4 assignments", details = ex.Message });
        }
    }
}
