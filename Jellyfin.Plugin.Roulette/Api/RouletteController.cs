using System;
using System.Globalization;
using System.Linq;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Roulette.Api;

/// <summary>
/// Roulette API controller.
/// </summary>
[ApiController]
[Authorize]
[Route("Roulette")]
public class RouletteController : ControllerBase
{
    // Claim names used by Jellyfin's authentication handler (Jellyfin.Api.Constants.InternalClaimTypes).
    private const string UserIdClaim = "Jellyfin-UserId";
    private const string AdministratorRole = "Administrator";

    private readonly IPlaylistManager _playlistManager;
    private readonly IUserManager _userManager;
    private readonly IDtoService _dtoService;
    private readonly ILogger<RouletteController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RouletteController"/> class.
    /// </summary>
    /// <param name="playlistManager">Instance of the <see cref="IPlaylistManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{RouletteController}"/> interface.</param>
    public RouletteController(
        IPlaylistManager playlistManager,
        IUserManager userManager,
        IDtoService dtoService,
        ILogger<RouletteController> logger)
    {
        _playlistManager = playlistManager;
        _userManager = userManager;
        _dtoService = dtoService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a random unwatched item from a playlist.
    /// </summary>
    /// <param name="playlistId">The playlist ID.</param>
    /// <param name="userId">Optional user ID. Defaults to the authenticated user; only administrators may pass another user.</param>
    /// <returns>A random unwatched playable item from the playlist.</returns>
    [HttpGet("Random/{playlistId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<BaseItemDto> GetRandomUnwatchedItem(
        [FromRoute] Guid playlistId,
        [FromQuery] Guid? userId)
    {
        var callerId = GetCallerUserId();
        var effectiveUserId = userId is { } requested && requested != Guid.Empty ? requested : callerId;

        if (effectiveUserId == Guid.Empty)
        {
            return BadRequest("User ID could not be determined");
        }

        if (effectiveUserId != callerId && !User.IsInRole(AdministratorRole))
        {
            return Forbid();
        }

        var user = _userManager.GetUserById(effectiveUserId);
        if (user is null)
        {
            _logger.LogWarning("[Roulette][WARN] User not found: {UserId}", effectiveUserId);
            return BadRequest("Invalid user ID");
        }

        var playlist = _playlistManager.GetPlaylistForUser(playlistId, effectiveUserId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        // Same access rules as Jellyfin's own GET /Playlists/{id}/Items.
        var isPermitted = playlist.OpenAccess
            || playlist.OwnerUserId.Equals(effectiveUserId)
            || playlist.Shares.Any(s => s.UserId.Equals(effectiveUserId));
        if (!isPermitted)
        {
            return Forbid();
        }

        // GetManageableItems returns the playlist entries themselves (movies, episodes, audio, videos)
        // regardless of item type, unlike a ParentId query filtered to movies.
        var unwatched = playlist.GetManageableItems()
            .Select(entry => entry.Item2)
            .Where(item => item is not null && !item.IsFolder && item.IsVisible(user) && !item.IsPlayed(user, null))
            .ToList();

        if (unwatched.Count == 0)
        {
            return NotFound("No unwatched items found in playlist");
        }

        var selectedItem = unwatched[Random.Shared.Next(unwatched.Count)];

        _logger.LogDebug(
            "[Roulette][DEBUG] Selected {ItemName} ({ItemId}) from {Count} unwatched items in playlist {PlaylistId}",
            selectedItem.Name,
            selectedItem.Id,
            unwatched.Count,
            playlistId);

        return Ok(_dtoService.GetBaseItemDto(selectedItem, new DtoOptions(), user));
    }

    private Guid GetCallerUserId()
    {
        var value = User.FindFirst(UserIdClaim)?.Value;
        return Guid.TryParse(value, CultureInfo.InvariantCulture, out var id) ? id : Guid.Empty;
    }
}
