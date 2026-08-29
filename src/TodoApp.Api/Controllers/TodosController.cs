using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Contracts;
using TodoApp.Core;

namespace TodoApp.Api.Controller
{
    [ApiController]
    [Route("api/todos")]
    public sealed class TodosController : ControllerBase
    {
        private readonly TodoService _service;

        public TodosController(TodoService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpPost]
        [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TodoResponse>> Create(
            CreateTodoRequest request,
            CancellationToken cancellationToken)
        {
            var item = await _service.CreateAsync(
                request.Title,
                request.Description,
                request.DueDate,
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = item.Id }, TodoMapper.ToResponse(item));
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<TodoResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<TodoResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var items = await _service.GetAllAsync(cancellationToken);

            return Ok(items.Select(TodoMapper.ToResponse).ToList());
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TodoResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var item = await _service.GetByIdAsync(id, cancellationToken);

            return Ok(TodoMapper.ToResponse(item));
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TodoResponse>> Update(
            Guid id,
            UpdateTodoRequest request,
            CancellationToken cancellationToken)
        {
            var item = await _service.UpdateAsync(
                id,
                request.Title,
                request.Description,
                request.DueDate,
                cancellationToken);

            return Ok(TodoMapper.ToResponse(item));
        }

        [HttpPost("{id}/complete")]
        [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<TodoResponse>> Complete(Guid id, CancellationToken cancellationToken)
        {
            var item = await _service.CompleteAsync(id, cancellationToken);

            return Ok(TodoMapper.ToResponse(item));
        }

        [HttpPost("{id}/incomplete")]
        [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<TodoResponse>> Incomplete(Guid id, CancellationToken cancellationToken)
        {
            var item = await _service.IncompleteAsync(id, cancellationToken);

            return Ok(TodoMapper.ToResponse(item));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);

            return NoContent();
        }
    }
}
