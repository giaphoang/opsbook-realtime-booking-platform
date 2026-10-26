using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalR.BusinessLayer.Abstract;
using SignalR.DtoLayer.BookingDto;
using SignalR.EntityLayer.Entities;
using SignalRApi.Hubs;

namespace SignalRApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Registrar")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateBookingDto> _createBookingValidator;
        private readonly IOperationEventBroadcaster _operationEventBroadcaster;

        public BookingController(IBookingService bookingService, IMapper mapper,
            IValidator<CreateBookingDto> createBookingValidator,
            IOperationEventBroadcaster operationEventBroadcaster)
        {
            _bookingService = bookingService;
            _mapper = mapper;
            _createBookingValidator = createBookingValidator;
            _operationEventBroadcaster = operationEventBroadcaster;
        }

        [HttpGet]
        public IActionResult BookingList()
        {
            var values = _bookingService.TGetListAll();
            var result = _mapper.Map<List<ResultBookingDto>>(values);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public IActionResult GetBooking(int id)
        {
            var value = _bookingService.TGetById(id);
            if (value is null)
            {
                return NotFound();
            }

            var result = _mapper.Map<GetBookingDto>(value);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking(CreateBookingDto dto)
        {
            var result = _createBookingValidator.Validate(dto);

            if (!result.IsValid)
            {
                return BadRequest(new
                {
                    Errors = result.Errors
                        .GroupBy(error => error.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(error => error.ErrorMessage).ToArray())
                });
            }

            var value = _mapper.Map<Booking>(dto);
            _bookingService.TAdd(value);

            var operationEvent = new OperationEvent("Booking", dto.Email, "Create");
            await _operationEventBroadcaster.PublishAsync(
                OperationsHub.OperationsRoom, operationEvent);
            return Ok(operationEvent);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var value = _bookingService.TGetById(id);
            if (value is null)
            {
                return NotFound();
            }

            _bookingService.TDelete(value);
            var operationEvent = new OperationEvent("Booking", value.Email, "Delete");
            await _operationEventBroadcaster.PublishAsync(
                OperationsHub.OperationsRoom, operationEvent);
            return Ok(operationEvent);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateBooking(UpdateBookingDto dto)
        {
            var existing = _bookingService.TGetById(dto.BookingID);
            if (existing is null)
            {
                return NotFound();
            }

            var value = _mapper.Map<Booking>(dto);
            _bookingService.TUpdate(value);

            var operationEvent = new OperationEvent("Booking", value.Email, "Update");
            await _operationEventBroadcaster.PublishAsync(
                OperationsHub.OperationsRoom, operationEvent);
            return Ok(operationEvent);
        }
    }
}
